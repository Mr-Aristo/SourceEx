# SourceEx

SourceEx is an expense approval focused `.NET` solution built around `Clean Architecture + Event-Driven` principles. The solution is split into `Domain`, `Application`, `Infrastructure`, `API`, `Worker`, and `BuildingBlocks` layers.

## Services

- `SourceEx.API`: Minimal API with JWT bearer authentication, API versioning, rate limiting, and the outbox publisher host.
- `SourceEx.Worker.Notification`: Listens to approval and risk assessment events.
- `SourceEx.Worker.Audit`: Records system events into the audit log flow.
- `SourceEx.Worker.Policy`: Consumes `ExpenseCreatedIntegrationEvent` messages and evaluates risk through Ollama.
- `SourceEx.Integrations.Ollama`: Refit-based Ollama client and AI policy integration layer.

## Infrastructure

The repository includes a root-level [docker-compose.yml](docker-compose.yml) for local infrastructure. It starts:

- PostgreSQL
- RabbitMQ Management
- Ollama

## Quick Start

1. Start the infrastructure:

```bash
docker compose up -d
```

2. Pull the Ollama model:

```bash
docker exec -it sourceex-ollama ollama pull gemma3
```

3. Run the API and worker projects in separate terminals:

```bash
dotnet run --project src/SourceEx.API
dotnet run --project src/SourceEx.Worker.Notification
dotnet run --project src/SourceEx.Worker.Audit
dotnet run --project src/SourceEx.Worker.Policy
```

Note: use the actual HTTP/HTTPS address printed by ASP.NET Core when the API starts. The examples below assume `http://localhost:5000`.

4. Generate a local JWT token:

```bash
curl -X POST http://localhost:5000/api/v1.0/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "employee-001",
    "departmentId": "finance",
    "roles": ["employee"]
  }'
```

For the approve endpoint, use a token that has one of these roles: `manager`, `finance`, or `admin`.

5. Create an expense:

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

Expense endpoints expect these claims:

- `user_id`
- `department_id`

The approve endpoint additionally requires one of these roles:

- `manager`
- `finance`
- `admin`

## Ollama Flow

`SourceEx.Worker.Policy` consumes `ExpenseCreatedIntegrationEvent` messages and uses Ollama's `/api/chat` endpoint to evaluate expense risk. If Ollama is unavailable, the worker continues the flow using deterministic fallback rules.

## Roadmap

Implemented and planned work is tracked in [ROADMAP.md](docs/ROADMAP.md).

## Documentation

Project documentation is available in:

- [Project Documentation](docs/PROJECT_DOCUMENTATION.md)
- [Testing Guide](docs/TESTING_GUIDE.md)
- [Linux Local Production Guide](docs/linux-local-production-tr.md)
- [Architecture Guide in Turkish](docs/architecture-tr.md)

## Client

The Angular client foundation lives in:

- [SourceEx Web Client](client/sourceex-web/README.md)
