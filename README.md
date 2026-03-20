# SourceEx

SourceEx is an expense approval focused `.NET` solution built around `Clean Architecture + Event-Driven` principles. The solution is split into `Domain`, `Application`, `Infrastructure`, `API`, `Worker`, `Identity`, and `BuildingBlocks` layers.

## Services

- `SourceEx.API`: Minimal API with JWT bearer authentication, API versioning, rate limiting, and the outbox publisher host.
- `SourceEx.Identity.API`: Dedicated identity module for login, password hashing, refresh tokens, role assignment, and JWT issuance.
- `SourceEx.Worker.Notification`: Listens to approval and risk assessment events.
- `SourceEx.Worker.Audit`: Records system events into the audit log flow.
- `SourceEx.Worker.Policy`: Consumes `ExpenseCreatedIntegrationEvent` messages and evaluates risk through Ollama.
- `SourceEx.Integrations.Ollama`: Refit-based Ollama client and AI policy integration layer.

## Infrastructure

The repository includes a root-level [docker-compose.yml](docker-compose.yml) for local infrastructure. It starts:

- PostgreSQL
- RabbitMQ Management
- Ollama
- Prometheus
- Grafana

## Quick Start

1. Start the infrastructure:

```bash
docker compose up -d
```

2. Pull the Ollama model:

```bash
docker exec -it sourceex-ollama ollama pull gemma3
```

3. Run the identity service and expense API on fixed HTTP ports so Prometheus can scrape them:

```bash
ASPNETCORE_URLS=http://localhost:5001 dotnet run --project src/SourceEx.Identity.API
ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/SourceEx.API
```

4. Run the worker projects in separate terminals:

```bash
dotnet run --project src/SourceEx.Worker.Notification
dotnet run --project src/SourceEx.Worker.Audit
dotnet run --project src/SourceEx.Worker.Policy
```

Note: use the actual HTTP/HTTPS addresses printed by ASP.NET Core when the services start. The examples below assume:

- identity service: `http://localhost:5001`
- expense API: `http://localhost:5000`

Local observability endpoints:

- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000` with `admin / sourceex`
- expense API metrics: `http://localhost:5000/metrics`
- identity API metrics: `http://localhost:5001/metrics`
- Grafana auto-loads the provisioned `SourceEx Overview` dashboard

5. Log in through the identity service:

```bash
curl -X POST http://localhost:5001/api/v1.0/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "userNameOrEmail": "employee-001",
    "password": "Passw0rd!"
  }'
```

Seeded local accounts:

- `employee-001` / `Passw0rd!`
- `manager-001` / `Passw0rd!`
- `finance-001` / `Passw0rd!`
- `admin-001` / `Passw0rd!`

Use `manager-001`, `finance-001`, or `admin-001` when you need an approver token.

6. Create an expense with the returned `accessToken`:

```bash
curl -X POST http://localhost:5000/api/v1.0/expenses \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 2750,
    "currency": "USD",
    "description": "Conference travel reimbursement"
  }'
```

## JWT Claims

Expense endpoints expect these claims inside the JWT issued by `SourceEx.Identity.API`:

- `user_id`
- `department_id`
- `display_name`

The approve endpoint additionally requires one of these roles:

- `manager`
- `finance`
- `admin`

## Ollama Flow

`SourceEx.Worker.Policy` consumes `ExpenseCreatedIntegrationEvent` messages and uses Ollama's `/api/chat` endpoint to evaluate expense risk. If Ollama is unavailable, the worker continues the flow using deterministic fallback rules.

## Observability

The minimum viable observability baseline now includes:

- structured JSON logging through Serilog
- request logging for both API hosts
- existing `/health/live` and `/health/ready` endpoints
- Prometheus metrics endpoints on the two API hosts
- Prometheus + Grafana containers with provisioned datasource and starter dashboard

This baseline does not yet include distributed tracing, Elasticsearch/Kibana, or centralized log search. Those remain future-phase concerns.

## Roadmap

Implemented and planned work is tracked in [ROADMAP.md](docs/ROADMAP.md).

## Architecture

Architecture-focused documentation is available in:

- [Architecture Guide (English)](docs/architecture-en.md)
- [Architecture Guide (Turkish)](docs/architecture-tr.md)
- [Roadmap](docs/ROADMAP.md)

## Documentation

Project documentation is available in:

- [Architecture Guide (English)](docs/architecture-en.md)
- [Architecture Guide (Turkish)](docs/architecture-tr.md)
- [Observability Guide](docs/OBSERVABILITY.md)
- [Project Documentation](docs/PROJECT_DOCUMENTATION.md)
- [Testing Guide](docs/TESTING_GUIDE.md)
- [Linux Local Production Guide](docs/linux-local-production-tr.md)

## Client

The Angular client foundation lives in:

- [SourceEx Web Client](client/sourceex-web/README.md)
