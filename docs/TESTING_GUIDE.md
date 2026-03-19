# SourceEx Testing Guide

## Purpose

This document explains how to test the SourceEx solution end to end, what infrastructure is required, what commands should be run, and what outcomes are expected at each step.

The current repository contains the application code and local infrastructure definitions, but it does not yet contain automated test projects or committed Entity Framework migrations. Because of that, the most reliable way to validate the system today is:

- local infrastructure startup
- database migration creation and application
- API and worker startup
- manual and API-driven smoke testing
- event flow verification through logs and RabbitMQ

## What Should Be Tested

The minimum validation scope for SourceEx should include:

- API authentication
- authorization policies
- expense creation
- expense retrieval
- expense approval
- domain validation
- outbox publishing
- RabbitMQ delivery
- audit worker processing
- notification worker processing
- policy worker processing
- Ollama-based risk assessment
- deterministic fallback behavior when Ollama is unavailable

## Prerequisites

Before testing, make sure the following tools are installed on your machine:

- .NET SDK 10.x
- Docker Desktop or Docker Engine with Compose
- Entity Framework Core CLI tools
- `curl` or Postman

Recommended command to install EF CLI if needed:

```bash
dotnet tool install --global dotnet-ef
```

## Required Infrastructure

The repository ships with [docker-compose.yml](../docker-compose.yml), which starts:

- PostgreSQL
- RabbitMQ with management UI
- Ollama

Start infrastructure:

```bash
docker compose up -d
```

Check container status:

```bash
docker compose ps
```

Download the Ollama model used by the policy worker:

```bash
docker exec -it sourceex-ollama ollama pull gemma3
```

## Database Preparation

At the current stage, the repository does not include committed EF Core migrations. Create them once before the first full test run.

Create the initial migration:

```bash
dotnet ef migrations add InitialCreate \
  --project src/SourceEx.Infrastructure \
  --startup-project src/SourceEx.API \
  --output-dir Data/Migrations
```

Apply the migration:

```bash
dotnet ef database update \
  --project src/SourceEx.Infrastructure \
  --startup-project src/SourceEx.API
```

## Build Validation

Run a solution build before starting services:

```bash
dotnet build SourceEx.slnx
```

This validates package restore, project references, and compile-time wiring.

## Starting the Services

Run the services in separate terminals:

```bash
dotnet run --project src/SourceEx.API
```

```bash
dotnet run --project src/SourceEx.Worker.Notification
```

```bash
dotnet run --project src/SourceEx.Worker.Audit
```

```bash
dotnet run --project src/SourceEx.Worker.Policy
```

Use the actual API base URL printed by ASP.NET Core when the API starts. The examples below use `http://localhost:5000`.

## Smoke Test Flow

### 1. Verify Health Endpoints

Liveness:

```bash
curl http://localhost:5000/health/live
```

Readiness:

```bash
curl http://localhost:5000/health/ready
```

Expected result:

- `live` should return success if the API process is running
- `ready` should return success only when the database health check passes

### 2. Issue an Employee Token

```bash
curl -X POST http://localhost:5000/api/v1.0/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "employee-001",
    "departmentId": "finance",
    "roles": ["employee"]
  }'
```

Expected result:

- a JWT bearer token
- an expiration timestamp

### 3. Issue an Approver Token

```bash
curl -X POST http://localhost:5000/api/v1.0/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "manager-001",
    "departmentId": "finance",
    "roles": ["manager"]
  }'
```

Expected result:

- a JWT bearer token with an approver-capable role

### 4. Verify Authenticated Identity

```bash
curl http://localhost:5000/api/v1.0/auth/me \
  -H "Authorization: Bearer <EMPLOYEE_TOKEN>"
```

Expected result:

- the `user_id`
- the `department_id`
- the assigned roles

### 5. Create an Expense

```bash
curl -X POST http://localhost:5000/api/v1.0/expenses \
  -H "Authorization: Bearer <EMPLOYEE_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 2750,
    "currency": "USD",
    "description": "Conference travel reimbursement"
  }'
```

Expected result:

- HTTP `201 Created`
- a new `expenseId`

What should happen internally:

- the expense aggregate is created
- a domain event is added
- an outbox row is written inside the database transaction
- the outbox processor publishes the integration event
- the policy worker consumes `ExpenseCreatedIntegrationEvent`
- the policy worker calls Ollama
- the policy worker publishes `ExpenseRiskAssessedIntegrationEvent`
- the audit worker records the event
- the notification worker logs a manual-review message if required

### 6. Retrieve the Expense

```bash
curl http://localhost:5000/api/v1.0/expenses/<EXPENSE_ID> \
  -H "Authorization: Bearer <EMPLOYEE_TOKEN>"
```

Expected result:

- HTTP `200 OK`
- the stored expense details
- `status` should still be `Pending`

### 7. Approve the Expense

```bash
curl -X POST http://localhost:5000/api/v1.0/expenses/<EXPENSE_ID>/approve \
  -H "Authorization: Bearer <APPROVER_TOKEN>"
```

Expected result:

- HTTP `204 No Content`

What should happen internally:

- authorization policy checks the caller role
- the domain aggregate moves from `Pending` to `Approved`
- `ExpenseApprovedIntegrationEvent` is written to outbox
- the outbox processor publishes it
- notification worker logs the approval handling
- audit worker logs the approval handling

## Validation and Negative Tests

The following checks should also be executed:

- create an expense without a token
  - expected: `401 Unauthorized`
- call `/approve` with a token that does not have `manager`, `finance`, or `admin`
  - expected: `403 Forbidden`
- create an expense with invalid payload such as empty currency or zero amount
  - expected: `400 Bad Request`
- approve an expense from a different department
  - expected: `400 Bad Request`
- request a non-existing expense ID
  - expected: `404 Not Found`

## RabbitMQ Verification

RabbitMQ management UI is available at:

```text
http://localhost:15672
```

Default credentials:

```text
guest / guest
```

Use the management UI to confirm:

- the bus is connected
- queues are created for each worker
- messages are flowing through the expected endpoints

## PostgreSQL Verification

Connect to PostgreSQL and verify:

- `Expenses` table contains the created expense
- `OutboxMessages` table contains integration events
- processed outbox rows have `ProcessedOnUtc` set

Example connection settings:

```text
Host=localhost
Port=5432
Database=sourceex
Username=postgres
Password=postgres
```

## Ollama Verification

The policy worker depends on Ollama at:

```text
http://localhost:11434
```

The worker is configured to use model:

```text
gemma3
```

Expected behavior:

- if Ollama is reachable, risk assessment is AI-generated
- if Ollama is unavailable, the worker falls back to deterministic rules and still emits a risk event

## Fallback Test for Ollama

To verify fallback behavior:

1. Stop the Ollama container:

```bash
docker stop sourceex-ollama
```

2. Create a new expense again.
3. Observe the policy worker logs.

Expected result:

- the worker should log a warning
- the flow should continue
- a fallback `ExpenseRiskAssessedIntegrationEvent` should still be published

After the test:

```bash
docker start sourceex-ollama
```

## Recommended Automated Test Strategy

The repository should eventually include the following automated test layers:

- Domain unit tests
  - aggregate behavior
  - value objects
  - invariant enforcement
- Application tests
  - validators
  - MediatR handlers
  - authorization-related orchestration
- Integration tests
  - API + PostgreSQL + RabbitMQ + workers
  - outbox publishing
  - authentication/authorization
- Contract tests
  - integration event schemas
  - Ollama response parsing
- Container-based tests
  - PostgreSQL via Testcontainers
  - RabbitMQ via Testcontainers
  - optional Ollama test double or local container

## Current Gaps

At the current state, keep these limitations in mind while testing:

- no committed EF Core migrations yet
- no automated test project yet
- no inbox/idempotency persistence yet
- no production-grade distributed tracing yet

## Testing Exit Criteria

A local test run can be considered successful when:

- the solution builds
- the database schema is applied successfully
- the API starts
- all workers start
- token issuance works
- expense creation works
- expense retrieval works
- expense approval works
- outbox rows are published
- RabbitMQ queues receive and process messages
- Ollama-powered risk assessment works
- deterministic fallback works when Ollama is offline
