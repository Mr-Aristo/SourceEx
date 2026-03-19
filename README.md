# SourceEx

SourceEx, expense approval odakli bir `Clean Architecture + Event-Driven` .NET cozumudur. Cozum; `Domain`, `Application`, `Infrastructure`, `API`, `Worker` ve `BuildingBlocks` katmanlarina ayrilir.

## Servisler

- `SourceEx.API`: Minimal API, JWT bearer auth, versioning, rate limiting ve outbox publisher host'u.
- `SourceEx.Worker.Notification`: Onay ve risk assessment event'lerini dinler.
- `SourceEx.Worker.Audit`: Sistem olaylarini audit log akisina yazar.
- `SourceEx.Worker.Policy`: `ExpenseCreatedIntegrationEvent` olaylarini alip Ollama ile risk analizi yapar.
- `SourceEx.Integrations.Ollama`: Refit tabanli Ollama istemcisi ve AI policy entegrasyonu.

## Altyapi

Local altyapi icin repo kokunde bir [docker-compose.yml](docker-compose.yml) bulunur. Bu dosya:

- PostgreSQL
- RabbitMQ Management
- Ollama

servislerini ayaga kaldirir.

## Hizli Baslangic

1. Altyapiyi baslat:

```bash
docker compose up -d
```

2. Ollama modeli indir:

```bash
docker exec -it sourceex-ollama ollama pull gemma3
```

3. API ve worker projelerini ayri terminallerde calistir:

```bash
dotnet run --project src/SourceEx.API
dotnet run --project src/SourceEx.Worker.Notification
dotnet run --project src/SourceEx.Worker.Audit
dotnet run --project src/SourceEx.Worker.Policy
```

Not: `dotnet run` sirasinda ASP.NET Core'un bastigi HTTP/HTTPS adresini kullan. Asagidaki `curl` ornekleri varsayilan olarak `http://localhost:5000` uzerinden verilmis durumda.

4. Local JWT token uret:

```bash
curl -X POST http://localhost:5000/api/v1.0/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "employee-001",
    "departmentId": "finance",
    "roles": ["employee"]
  }'
```

Approve endpoint'i icin `manager`, `finance` veya `admin` rolune sahip bir token kullan.

5. Expense olustur:

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

Expense endpoint'leri su claim'leri bekler:

- `user_id`
- `department_id`

Approve endpoint'i ek olarak su rollerden birini ister:

- `manager`
- `finance`
- `admin`

## Ollama Akisi

`SourceEx.Worker.Policy`, `ExpenseCreatedIntegrationEvent` olaylarini tuketir ve Ollama'nin `/api/chat` endpoint'ini kullanarak risk degerlendirmesi yapar. Ollama erisilemezse worker deterministic fallback kurallariyla akisi durdurmadan devam eder.

## Roadmap

Uygulanan ve planlanan adimlar [ROADMAP.md](docs/ROADMAP.md) dosyasinda tutulur.

## Documentation

English project documentation is available in:

- [Project Documentation](docs/PROJECT_DOCUMENTATION.md)
- [Testing Guide](docs/TESTING_GUIDE.md)

## Client

The Angular client foundation lives in:

- [SourceEx Web Client](client/sourceex-web/README.md)
