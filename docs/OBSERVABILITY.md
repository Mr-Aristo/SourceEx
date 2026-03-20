# SourceEx Observability Guide

This document describes the minimum viable observability baseline currently implemented in SourceEx.

## Implemented Baseline

The current repository now includes:

- structured JSON logging through Serilog in all API and worker hosts
- request logging in `SourceEx.API` and `SourceEx.Identity.API`
- existing `/health/live` and `/health/ready` endpoints in both API hosts
- Prometheus metrics endpoints exposed at `/metrics` by both API hosts
- custom application metrics for:
  - outbox publishing throughput, failures, and pending backlog
  - identity registrations
  - identity login attempts by result
  - identity refresh token requests by result
- Prometheus container for scraping local metrics
- Grafana container with provisioned Prometheus datasource and starter dashboard

## Intentionally Not Included Yet

The current baseline does not try to solve every observability concern at once.

Still future work:

- distributed tracing
- OpenTelemetry instrumentation and exporters
- centralized log search
- Elasticsearch/Kibana
- worker-hosted metrics endpoints
- advanced dashboard coverage for RabbitMQ, PostgreSQL, and Ollama

## Local Development Flow

Start the infrastructure:

```bash
docker compose up -d
```

Run the two API hosts on the ports expected by Prometheus:

```bash
ASPNETCORE_URLS=http://localhost:5001 dotnet run --project src/SourceEx.Identity.API
ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/SourceEx.API
```

Run the workers:

```bash
dotnet run --project src/SourceEx.Worker.Notification
dotnet run --project src/SourceEx.Worker.Audit
dotnet run --project src/SourceEx.Worker.Policy
```

Useful URLs:

- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`
- expense API metrics: `http://localhost:5000/metrics`
- identity API metrics: `http://localhost:5001/metrics`

Grafana credentials:

- user: `admin`
- password: `sourceex`

## What the Starter Dashboard Shows

The provisioned `SourceEx Overview` dashboard focuses on high-signal startup metrics:

- expense API target availability
- identity API target availability
- outbox pending messages
- outbox publish rate
- outbox failure rate
- identity login attempt rate by result
- identity refresh request rate by result
- self-service registration count over the last 24 hours

## Operational Notes

- Prometheus scrapes the API hosts through `host.docker.internal`, so fixed local ports are important.
- Worker processes currently contribute structured logs, but not their own separate Prometheus scrape targets.
- Health checks are still endpoint-based rather than converted into a full health dashboard model.

## Recommended Next Step

If observability should grow beyond this baseline, the next most valuable phase is:

1. OpenTelemetry-based tracing
2. richer dashboards
3. centralized log storage/search

That keeps the current low-cost baseline while opening the door to deeper diagnostics later.
