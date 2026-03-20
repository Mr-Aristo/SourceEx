# SourceEx Project Documentation

This document is the general English project guide. For the architecture-focused walkthrough, see [architecture-en.md](architecture-en.md).

## Overview

SourceEx is a modular .NET solution designed around:

- Clean Architecture
- Domain-Driven Design
- CQRS
- event-driven integration
- dedicated identity management
- transactional outbox
- worker-based asynchronous processing
- local AI-assisted policy evaluation through Ollama

The project models an expense management flow where:

1. a user authenticates through the identity module
2. the identity module issues a JWT access token and refresh token
3. the client calls the expense API with the issued JWT
4. the API stores the expense and writes an outbox event
5. background publishing forwards the event to RabbitMQ
6. workers react asynchronously
7. a policy worker evaluates the expense risk through Ollama
8. audit and notification workers react to downstream events

## Solution Structure

The solution is composed of the following projects.

### BuildingBlocks

`BuildingBlocks` contains reusable cross-cutting primitives shared by multiple projects.

Main responsibilities:

- CQRS abstractions
- command and query handler interfaces
- shared integration event base type
- MassTransit and RabbitMQ registration helpers

### SourceEx.Domain

`SourceEx.Domain` contains the core business model.

Main responsibilities:

- entities and aggregates
- value objects
- domain exceptions
- domain events
- business rules and invariants

The `Expense` aggregate is the center of the domain model.

### SourceEx.Application

`SourceEx.Application` contains use cases and request processing behavior.

Main responsibilities:

- command handlers
- query handlers
- FluentValidation validators
- MediatR pipeline behaviors
- application-layer exceptions

The application layer coordinates work but should not contain infrastructure concerns.

### SourceEx.Infrastructure

`SourceEx.Infrastructure` contains persistence and durable messaging concerns.

Main responsibilities:

- Entity Framework Core DbContext
- entity configuration
- outbox persistence
- outbox processor
- database health checks
- database service registration

### SourceEx.Contracts

`SourceEx.Contracts` contains cross-service event contracts.

Main responsibilities:

- expense integration events
- event messages shared between API and workers

These contracts define the shape of messages exchanged over RabbitMQ.

### SourceEx.API

`SourceEx.API` is the HTTP entry point.

Main responsibilities:

- minimal API endpoints
- JWT authentication
- authorization policies
- API versioning
- rate limiting
- exception-to-HTTP mapping
- health endpoints

The API is intentionally thin. It translates HTTP requests into application commands and queries.

### SourceEx.Identity.API

`SourceEx.Identity.API` is a dedicated identity module and should be treated as a separate service boundary.

Main responsibilities:

- user registration
- login and password verification
- refresh token rotation
- role-aware user management
- JWT issuance
- local bootstrap users for development

The expense API does not own usernames, passwords, or refresh tokens anymore. It only validates JWTs that were issued by this module.

This identity service is intentionally pragmatic in its current form. It already gives the solution a separate identity boundary, but it does not yet mirror the same `Domain / Application / Infrastructure` layering used by the expense module.

### SourceEx.Worker.Notification

`SourceEx.Worker.Notification` handles notification-oriented event processing.

Main responsibilities:

- react to approved expenses
- react to risky expenses that require manual review
- simulate notification behavior through logs

### SourceEx.Worker.Audit

`SourceEx.Worker.Audit` records operational audit events.

Main responsibilities:

- consume expense lifecycle events
- log audit-friendly event traces

### SourceEx.Integrations.Ollama

`SourceEx.Integrations.Ollama` contains the external AI integration.

Main responsibilities:

- Refit-based HTTP client for Ollama
- resilience policies for outbound AI calls
- expense risk assessment service
- deterministic fallback logic when Ollama is unavailable

### SourceEx.Worker.Policy

`SourceEx.Worker.Policy` is the AI-enabled policy engine worker.

Main responsibilities:

- consume `ExpenseCreatedIntegrationEvent`
- evaluate risk through Ollama
- emit `ExpenseRiskAssessedIntegrationEvent`

## Architecture Style

SourceEx follows a layered dependency flow:

- `Domain` depends on nothing application-specific or infrastructure-specific
- `Application` depends on `Domain` and shared abstractions
- `Infrastructure` depends on `Application`, `Domain`, and contracts
- `API` depends on `Application`, `Infrastructure`, contracts, and presentation concerns
- `Identity.API` depends on shared security primitives and its own persistence model
- `Workers` depend on contracts, shared messaging, and integration libraries

This keeps business logic independent from transport and infrastructure details.

## Core Business Flow

### Expense Creation Flow

1. A client obtains a JWT token from `SourceEx.Identity.API`.
2. The client calls `POST /api/v1.0/expenses`.
3. The API extracts `user_id` and `department_id` from the JWT claims.
4. A `CreateExpenseCommand` is sent through MediatR.
5. Application validation runs through the validation pipeline.
6. The `Expense` aggregate is created in the domain.
7. A domain event is added to the aggregate.
8. `ApplicationDbContext` converts domain events into integration events and stores them in `OutboxMessages`.
9. The transaction commits.
10. `OutboxProcessor` publishes pending outbox messages to RabbitMQ.
11. Workers consume the message asynchronously.

### Expense Approval Flow

1. An authorized caller invokes `POST /api/v1.0/expenses/{id}/approve`.
2. Authorization policy ensures the caller has an approver-capable role.
3. A command is sent to the application layer.
4. The domain aggregate verifies the department and current state.
5. The aggregate moves from `Pending` to `Approved`.
6. An approval domain event is generated.
7. The event is written to outbox and later published.
8. Audit and notification workers react asynchronously.

### AI Risk Assessment Flow

1. `SourceEx.Worker.Policy` consumes `ExpenseCreatedIntegrationEvent`.
2. The worker invokes the Ollama integration service.
3. Ollama returns a structured risk assessment payload.
4. The worker normalizes the result and publishes `ExpenseRiskAssessedIntegrationEvent`.
5. Audit and notification workers consume the event.
6. If Ollama is unavailable, deterministic fallback rules are used instead of failing the flow.

### Identity Flow

1. A client calls `POST /api/v1.0/identity/auth/login` with username or email plus password.
2. `SourceEx.Identity.API` verifies the hashed password against its own user store.
3. The identity service loads assigned roles and department information.
4. A JWT access token is issued with `user_id`, `department_id`, `display_name`, and role claims.
5. A refresh token is generated, hashed, and stored durably.
6. The client uses the access token when calling `SourceEx.API`.

## Authentication and Authorization

The solution now has a dedicated identity module instead of issuing development tokens from the expense API.

Important claims:

- `user_id`
- `department_id`
- `display_name`

Important roles:

- `employee`
- `manager`
- `finance`
- `admin`

Current policy model:

- authenticated expense access requires `user_id` and `department_id`
- approval additionally requires one of the privileged roles

Current login capabilities:

- self-service employee registration
- username/email + password login
- refresh token rotation
- local seeded worker, manager, finance, and admin users

Already implemented as minimum hardening:

- login, registration, and refresh token rate limiting
- account lockout after repeated failed attempts
- stronger password policy enforcement
- refresh token cleanup strategy

Still missing from the current implementation:

- deeper brute-force telemetry
- richer security auditing
- MFA and advanced account protection workflows

This can later be replaced or extended with Keycloak or another external identity provider, but the current structure already separates identity concerns from the expense domain.

## Validation Model

Validation is handled in the application layer, not directly in the API layer.

Current approach:

- request enters the API
- command or query is created
- MediatR pipeline executes FluentValidation validators
- validation errors are turned into `HttpValidationProblemDetails`

Benefits:

- validation is transport-agnostic
- the same rules apply outside HTTP if the same commands are used elsewhere
- endpoint code stays thin

## Messaging Model

SourceEx uses RabbitMQ and MassTransit for asynchronous communication.

Main ideas:

- domain events are internal business events
- integration events are external message contracts
- the outbox pattern bridges database writes and broker publishing

Current message families include:

- `ExpenseCreatedIntegrationEvent`
- `ExpenseApprovedIntegrationEvent`
- `ExpenseRejectedIntegrationEvent`
- `ExpenseRiskAssessedIntegrationEvent`

## Persistence Model

The current persistence strategy is PostgreSQL + Entity Framework Core.

Stored data includes:

- expenses
- outbox messages
- identity users
- identity roles
- identity refresh tokens

Current schema management now has an initial migration baseline:

- the repository contains committed EF Core migrations for both contexts
- the expense database is updated through `Database.MigrateAsync()` during startup
- the identity database is updated through `Database.MigrateAsync()` inside the identity startup flow

This is a meaningful improvement, but migration governance is still basic. Release sequencing, rollback discipline, and environment-specific rollout practices are still future work.

The current design intentionally keeps write-side persistence simple. Future evolution may include:

- dedicated read models
- Marten for event storage and projections
- inbox/idempotency persistence
- saga persistence for long-running workflows

## AI Integration Model

The Ollama integration is isolated from the domain and application core.

Why this matters:

- AI is an infrastructure concern
- policy evaluation can change independently of core business logic
- fallback behavior can be implemented without infecting the rest of the system

The current policy worker asks Ollama to return structured JSON with:

- risk level
- manual review flag
- confidence score
- reasoning

If Ollama fails, the system still emits a risk event using deterministic heuristics.

## Local Infrastructure

The repository includes `docker-compose.yml` for local development.

Local services:

- PostgreSQL
- RabbitMQ with management UI
- Ollama

The PostgreSQL container now also initializes the dedicated `sourceex_identity` database for the identity module during first boot.

This allows running the system without external cloud dependencies.

## Configuration

The most important configuration sections are:

- `ConnectionStrings:Database`
- `MessageBroker`
- `Jwt`
- `IdentitySeed`
- `Ollama`

These are defined in the host projects through `appsettings.json`.

## Operational Notes

### Health Checks

The API exposes:

- `/health/live`
- `/health/ready`

The readiness endpoint currently checks database connectivity.

### Logging

Workers and API currently rely on standard .NET logging.

Baseline observability that already exists:

- correlation ID middleware in both API hosts
- correlation IDs added to `ProblemDetails`
- activity tracking in host logging
- worker and outbox logs include message/correlation identifiers

This is enough for local development, but the recommended next step is to add:

- structured logging
- centralized log storage
- OpenTelemetry tracing and metrics

### Reverse Proxy Readiness

The repository includes deployment guidance that assumes Nginx or another reverse proxy, and the host applications now include a baseline reverse proxy integration.

Current reality:

- both hosts use `UseHttpsRedirection()`
- both hosts use `UseForwardedHeaders()`

This should be treated as a baseline rather than a finished hardening story. Trusted proxy/network rules and environment-specific forwarding policies still need review.

## Current Known Limitations

The current implementation is intentionally pragmatic and still has open areas for hardening:

- migration strategy exists but is still at an early governance level
- automated test projects now exist for domain and application baselines
- test coverage is still narrow
- no inbox/idempotency persistence yet
- reverse proxy support is baseline-only, not fully hardened
- identity hardening exists at a minimum viable level, but deeper protection is still missing
- no structured observability stack yet
- identity module does not yet mirror the same internal layering as the expense module
- no distributed tracing yet
- no dedicated read-model store yet
- no production identity provider integration yet

## Recommended Next Steps

Recommended next improvements:

- mature migration rollout practices beyond the initial baseline
- expand domain and application tests, then add infrastructure and API coverage
- harden reverse proxy trust settings for production-like deployments
- continue hardening the identity module
- add Testcontainers-based infrastructure tests after the basic test baseline exists
- add inbox/idempotency persistence
- consider MassTransit transactional outbox integration after migrations and tests stabilize
- add structured logging and OpenTelemetry
- evaluate whether the identity module should evolve toward its own `Application / Infrastructure` split
- introduce dedicated read models
- evaluate Marten when event sourcing or advanced projections become necessary

## Related Documents

- [Architecture Guide (English)](architecture-en.md)
- [Architecture Guide (Turkish)](architecture-tr.md)
- [Testing Guide](TESTING_GUIDE.md)
- [Roadmap](ROADMAP.md)
