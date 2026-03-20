# SourceEx Architecture Guide

This document explains the current SourceEx architecture as it exists in the repository today. It is intentionally grounded in the actual codebase rather than an idealized Clean Architecture template.

The goal is to help a new engineer understand:

- what is already implemented
- which architectural decisions are intentional
- which areas are still pragmatic shortcuts
- which improvements are planned rather than already present

## 1. Architectural Intent

SourceEx is an expense approval solution built around:

- Clean Architecture
- tactical DDD patterns
- CQRS-style request handling
- event-driven background processing
- a dedicated identity service

At a high level, the system works like this:

1. a user authenticates through `SourceEx.Identity.API`
2. the identity service issues JWT access and refresh tokens
3. the client calls `SourceEx.API`
4. the expense API persists the business change and writes an outbox record
5. a background outbox processor publishes integration events to RabbitMQ
6. workers consume those events for audit, notification, and AI-assisted policy evaluation

This means the repository is not just "an API plus a database". It is a layered write-side application with asynchronous worker processing and a separate identity boundary.

## 2. Current State vs Target State

It is important to distinguish between the architecture target and the current implementation.

Already implemented:

- layered `Domain`, `Application`, `Infrastructure`, and API projects
- expense aggregate with domain behavior
- FluentValidation pipeline in the application layer
- manual outbox pattern for reliable event publishing
- RabbitMQ + MassTransit consumers
- dedicated identity host for login, password hashing, roles, and refresh tokens
- Ollama integration isolated behind its own integration library
- committed initial EF Core migrations for the expense and identity stores
- startup-time schema application through `MigrateAsync()`
- reverse proxy baseline through forwarded header handling
- initial domain and application test projects
- basic observability foundation with correlation IDs and broker message correlation
- structured Serilog logging across API and worker hosts
- Prometheus metrics endpoints and local Grafana/Prometheus baseline

Not fully implemented yet:

- inbox/idempotency persistence
- broader migration rollout governance
- hardened reverse proxy configuration for trusted networks and HTTPS termination
- broader automated test coverage beyond the initial unit-test baseline
- full observability stack with tracing and centralized log search
- a fully layered identity module matching the rest of the solution

So the project is architecturally serious, but still intentionally pragmatic in a few places.

## 3. Repository Structure

The main projects are:

```text
src/
├─ BuildingBlocks
├─ SourceEx.Domain
├─ SourceEx.Application
├─ SourceEx.Infrastructure
├─ SourceEx.Contracts
├─ SourceEx.API
├─ SourceEx.Identity.API
├─ SourceEx.Worker.Notification
├─ SourceEx.Worker.Audit
├─ SourceEx.Worker.Policy
└─ SourceEx.Integrations.Ollama
```

### `BuildingBlocks`

Shared technical primitives:

- CQRS abstractions
- handler interfaces
- message broker registration helpers
- shared security constants such as claim and role names

### `SourceEx.Domain`

The business core:

- `Expense` aggregate
- value objects such as `ExpenseId` and `Money`
- domain events
- domain exceptions

### `SourceEx.Application`

The use case layer:

- commands and queries
- handlers
- FluentValidation validators
- MediatR pipeline behaviors
- application-level exceptions

### `SourceEx.Infrastructure`

Technical persistence and messaging concerns:

- EF Core DbContext
- entity mapping
- outbox storage
- outbox processor
- database bootstrap helpers

### `SourceEx.Contracts`

Integration event contracts shared across hosts and workers.

### `SourceEx.API`

The expense-focused HTTP entry point:

- minimal API endpoints
- JWT validation
- authorization policies
- rate limiting
- exception-to-HTTP mapping

### `SourceEx.Identity.API`

The identity service:

- registration
- login
- refresh token rotation
- role-aware user management
- JWT issuance

### Worker Projects

- `SourceEx.Worker.Notification`
- `SourceEx.Worker.Audit`
- `SourceEx.Worker.Policy`

These are background consumers rather than HTTP hosts.

### `SourceEx.Integrations.Ollama`

The isolated outbound AI integration layer.

## 4. Layer Responsibilities

### Domain

The domain layer owns business rules. `Expense` is not a passive data container. It contains behavior such as approval and rejection rules and emits domain events.

This is one of the stronger parts of the architecture because the business model is not pushed into handlers or controllers.

### Application

The application layer coordinates use cases such as:

- create expense
- approve expense
- get expense by id

It does not know about HTTP or RabbitMQ directly. It depends on abstractions such as `IApplicationDbContext`.

### Infrastructure

Infrastructure answers the question "how do we persist and publish this?" rather than "what is the business rule?"

This is where EF Core, outbox persistence, and background publishing live.

### API

The expense API is intentionally thin. It maps HTTP requests to commands and queries, delegates execution to MediatR, and maps failures to `ProblemDetails`.

### Identity API

The identity service is a pragmatic module. It is separated as a service boundary, which is good, but internally it does not yet mirror the same `Domain/Application/Infrastructure` layering used by the expense side.

Instead, it currently uses:

- minimal API endpoints
- EF Core directly in endpoint handlers
- manual validation helpers
- direct password hashing and refresh token workflows

This is a reasonable short-term choice, but it is also an explicit architectural trade-off.

## 5. Dependency Direction

The dependency flow is mostly aligned with Clean Architecture:

```text
Domain
  ^
  |
Application
  ^
  |
Infrastructure

API ----------> Application
API ----------> Infrastructure
API ----------> Contracts
API ----------> BuildingBlocks

Workers ------> Contracts
Workers ------> BuildingBlocks
Policy Worker -> Integrations.Ollama
```

What this gets right:

- business rules are not coupled to HTTP
- infrastructure depends on application abstractions
- workers do not carry unnecessary application logic

What is more pragmatic:

- `SourceEx.API` still references infrastructure directly at composition-root level
- `SourceEx.Identity.API` is intentionally separate as a host, but internally is not layered the same way yet

## 6. Expense Write Flow

The most representative flow is expense creation:

1. the client calls `POST /api/v1.0/expenses`
2. the API reads `user_id` and `department_id` from JWT claims
3. a `CreateExpenseCommand` is sent through MediatR
4. FluentValidation runs in the application pipeline
5. the handler creates an `Expense` aggregate
6. the aggregate adds a domain event
7. `ApplicationDbContext.SaveChangesAsync()` stores both data and outbox records
8. `OutboxProcessor` later publishes the integration event to RabbitMQ
9. workers consume the event asynchronously

This flow demonstrates the intended separation between synchronous write handling and asynchronous side effects.

## 7. Messaging and Outbox Model

The outbox implementation is one of the key architectural ideas in the repository.

Current behavior:

- domain events are collected from aggregates during `SaveChangesAsync()`
- they are mapped into integration events
- integration events are written into `OutboxMessages`
- a background processor polls pending rows and publishes them through MassTransit

This is a good educational and practical starting point, but it is still a manual outbox implementation.

Current limitations:

- no inbox persistence
- no durable idempotency strategy
- no multi-instance coordination for outbox polling
- no move to the built-in MassTransit transactional outbox yet

So the pattern is present, but the hardening work is still ahead.

## 8. Persistence Strategy

The current persistence model is PostgreSQL + EF Core.

Stored data includes:

- expenses
- outbox messages
- identity users
- identity roles
- identity refresh tokens

Initial schema lifecycle management is now in place.

Current reality:

- the repository contains committed initial migration folders for both EF Core contexts
- the expense side applies schema changes through `Database.MigrateAsync()` in `InfrastructureBootstrapper`
- the identity side applies schema changes through `Database.MigrateAsync()` in `IdentityDataSeeder`

This is a meaningful improvement over ad-hoc schema creation, but it is still an early-stage migration strategy. Rollback policy, release discipline, and environment-specific migration governance are still future work.

## 9. Reverse Proxy and Production Readiness

The documentation already assumes Nginx-style deployments, and the codebase now includes the first proxy-readiness step.

Current reality:

- both API hosts call `UseHttpsRedirection()`
- both API hosts call `UseForwardedHeaders()`

Why this matters:

- reverse proxy deployments often terminate HTTPS before Kestrel
- forwarded headers improve scheme detection and proxy-aware behavior
- client IP and trusted proxy handling still need production-specific hardening

So reverse proxy support is now present at a baseline level, but it is not fully hardened yet. `KnownProxies`, `KnownNetworks`, and deployment-specific trust rules should still be reviewed before a real production rollout.

## 10. Identity Architecture and Hardening Gaps

The identity service is already useful and functional:

- self-service employee registration
- username or email login
- hashed passwords
- refresh token rotation
- role-aware access

Current hardening status:

- login rate limiting and endpoint-level throttling are implemented
- account lockout and failed-attempt tracking are implemented
- password policy validation is enforced
- expired refresh token cleanup runs in the background
- brute-force telemetry depth and broader security auditing are still future work
- administrative and security auditing depth

Architecturally, the identity host is also more pragmatic than the expense side. It currently trades strict layering for implementation speed and clarity.

## 11. Testing and Observability

### Testing

The repository now contains an initial automated test baseline.

Current reality:

- there are dedicated test projects for the domain and application layers
- the initial suite covers aggregate behavior and validator behavior
- there are still no infrastructure integration tests
- no Testcontainers-based environment tests yet

Because of that, the next testing step is expansion rather than creation from scratch: broader handler tests, API-level tests, and containerized integration coverage should follow the current unit-test baseline.

### Observability

The codebase now has a true minimum observability baseline rather than just ad-hoc host logs.

Already present:

- structured Serilog JSON logging in API and worker hosts
- request logging in both API hosts
- correlation ID middleware in both API hosts
- correlation IDs added to `ProblemDetails`
- activity tracking configuration in the host logging pipeline
- broker message and correlation identifiers included in worker and outbox logs
- Prometheus metrics endpoints in both API hosts
- custom application metrics for outbox and identity flows
- local Prometheus and Grafana support with provisioned dashboards

What is still missing:

- centralized log storage
- distributed tracing
- OpenTelemetry
- direct worker metrics endpoints
- richer infrastructure dashboards and metrics

For an event-driven system, this gap is worth calling out explicitly.

## 12. Honest Architectural Assessment

What the project does well today:

- clear solution modularity
- business behavior inside the domain model
- application-layer validation via pipeline behavior
- separate identity boundary
- outbox-backed asynchronous integration
- isolated AI integration

Where the project is still intentionally incomplete:

- migration governance is still basic even though initial migrations exist
- reverse proxy readiness is at baseline level, not full production hardening
- identity hardening is partial
- the identity module is not yet layered like the expense module
- automated tests exist, but coverage is still narrow
- observability is present at a useful baseline, but not yet full-spectrum
- inbox/idempotency is missing

This is not a contradiction. It simply means the project is a strong architectural base that still has a visible hardening backlog.

## 13. Recommended Near-Term Focus

Based on the current codebase, the most valuable near-term improvements are:

1. mature the migration workflow beyond the initial baseline
2. harden reverse proxy behavior for trusted networks and HTTPS termination
3. broaden tests from unit coverage to handler, API, and containerized integration flows
4. continue hardening the identity service
5. add inbox/idempotency support
6. improve observability beyond correlation-aware logging

More advanced options such as dedicated read models, Marten, or saga state machines should stay in the later-growth category rather than the immediate backlog.

## 14. Related Documents

- [Architecture Guide in Turkish](architecture-tr.md)
- [Project Documentation](PROJECT_DOCUMENTATION.md)
- [Roadmap](ROADMAP.md)
- [Testing Guide](TESTING_GUIDE.md)
