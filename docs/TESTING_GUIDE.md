# SourceEx Testing Guide

## Purpose

This document explains how to test the SourceEx solution end to end, what infrastructure is required, what commands should be run, and what outcomes are expected at each step.

The current repository contains the application code and local infrastructure definitions, but it does not yet contain automated test projects or committed Entity Framework migrations. Because of that, the most reliable way to validate the system today is:

- local infrastructure startup
- automatic schema bootstrap through `EnsureCreated`
- API and worker startup
- identity service startup
- manual and API-driven smoke testing
- event flow verification through logs and RabbitMQ

## What Should Be Tested

The minimum validation scope for SourceEx should include:

- identity registration and login
- JWT issuance and refresh
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

On the first boot, PostgreSQL also runs an init script that creates the dedicated `sourceex_identity` database used by the identity module.

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

The current codebase does not include committed EF Core migrations yet. Instead, both the expense API and the identity service bootstrap their local schema with `EnsureCreated()` during startup.

What this means in practice:

- for local validation, you can start the services without generating migrations first
- for long-term production readiness, proper migrations should still be added later
- if PostgreSQL data volumes already existed before the identity module was introduced, recreate the database volume or create `sourceex_identity` manually

## Build Validation

Run a solution build before starting services:

```bash
dotnet build SourceEx.slnx
```

This validates package restore, project references, and compile-time wiring.

## Starting the Services

Run the services in separate terminals:

```bash
dotnet run --project src/SourceEx.Identity.API
```

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

Use the actual base URLs printed by ASP.NET Core when the services start. The examples below assume:

- identity service: `http://localhost:5001`
- expense API: `http://localhost:5000`

## Smoke Test Flow

### 1. Verify Health Endpoints

Identity liveness:

```bash
curl http://localhost:5001/health/live
```

Identity readiness:

```bash
curl http://localhost:5001/health/ready
```

Expense API liveness:

```bash
curl http://localhost:5000/health/live
```

Expense API readiness:

```bash
curl http://localhost:5000/health/ready
```

Expected result:

- both `live` endpoints should return success if the corresponding process is running
- both `ready` endpoints should return success only when the corresponding database health check passes

### 2. Log In as a Seeded Employee

```bash
curl -X POST http://localhost:5001/api/v1.0/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "userNameOrEmail": "employee-001",
    "password": "Passw0rd!"
  }'
```

Expected result:

- a JWT bearer token
- a refresh token
- the authenticated user profile
- role `employee`

### 3. Log In as a Seeded Approver

```bash
curl -X POST http://localhost:5001/api/v1.0/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "userNameOrEmail": "manager-001",
    "password": "Passw0rd!"
  }'
```

Expected result:

- a JWT bearer token with an approver-capable role
- role `manager`
- department `operations`

Other seeded accounts:

- `finance-001 / Passw0rd!`
- `admin-001 / Passw0rd!`

### 4. Verify Authenticated Identity Through the Identity Service

```bash
curl http://localhost:5001/api/v1.0/identity/auth/me \
  -H "Authorization: Bearer <EMPLOYEE_TOKEN>"
```

Expected result:

- the `user_id`
- the `department_id`
- the `displayName`
- the assigned roles

### 5. Verify the Same Token Against the Expense API

```bash
curl http://localhost:5000/api/v1.0/auth/me \
  -H "Authorization: Bearer <EMPLOYEE_TOKEN>"
```

Expected result:

- the expense API accepts the JWT issued by `SourceEx.Identity.API`
- the same user, department, and roles are visible to the business API

### 6. Create an Expense

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

### 7. Retrieve the Expense

```bash
curl http://localhost:5000/api/v1.0/expenses/<EXPENSE_ID> \
  -H "Authorization: Bearer <EMPLOYEE_TOKEN>"
```

Expected result:

- HTTP `200 OK`
- the stored expense details
- `status` should still be `Pending`

### 8. Approve the Expense

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
- call `POST /api/v1.0/identity/auth/login` with the wrong password
  - expected: `401 Unauthorized`
- call `POST /api/v1.0/identity/auth/register` twice with the same username or email
  - expected: `409 Conflict`
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
- `IdentityUsers` contains the seeded and registered users
- `IdentityRoles` contains `employee`, `manager`, `finance`, and `admin`
- `IdentityRefreshTokens` contains issued refresh tokens

Example connection settings:

```text
Host=localhost
Port=5432
Database=sourceex
Username=postgres
Password=postgres
```

Identity database:

```text
Host=localhost
Port=5432
Database=sourceex_identity
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
