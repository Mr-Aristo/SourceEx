# SourceEx Roadmap

Bu roadmap, mevcut Clean Architecture iskeletini bozmeden projeyi uretime daha yakin hale getirmek icin secilen uygulama planidir.

## Faz 1: Security Baseline

- [x] API'ye JWT bearer authentication ekle
- [x] Authorization policy'leri tanimla
- [x] Login, refresh token ve rol yonetimi icin ayri bir `SourceEx.Identity.API` modulu ekle
- [x] Expense endpoint'lerini identity servisi tarafindan uretilen token claim'leriyle calisir hale getir
- [x] Identity security hardening backlog'unun minimum uygulanabilir kismini ele al: login rate limiting, account lockout, password policy ve refresh token hygiene

## Faz 2: AI Policy Workflow

- [x] Ollama entegrasyonu icin ayri bir integration kutuphanesi ekle
- [x] Refit tabanli HTTP client tanimla
- [x] HTTP resilience ekle
- [x] ExpenseCreated event'ini tuketip risk degerlendirmesi yapan policy worker ekle
- [x] AI sonucunu yeni bir integration event olarak sisteme geri yayinla

## Faz 3: Delivery ve Architecture Hardening

- [x] EF migration stratejisini kur ve schema degisimlerini source control'e alinabilir hale getir
- [ ] Migration rollout ve release disiplini icin environment bazli uygulama/geri alma stratejisini netlestir
- [x] Reverse proxy readiness icin forwarded headers ve proxy-aware host ayarlarinin temelini ekle
- [ ] Forwarded headers, known proxy/network ve HTTPS termination senaryolarini production sertliginde tamamla
- [x] Domain/Application odakli unit test stratejisini baslat ve temel kapsami olustur
- [ ] Handler ve API davranislarini kapsayan daha genis test stratejisini oturt
- [ ] Identity modulu icin mevcut pragmatik endpoint + EF yapisinin uzun vadede `Application / Infrastructure` ayrimina alinip alinmayacagini degerlendir

## Faz 4: Messaging Hardening

- [x] AI sonuc event'ini audit ve notification worker'larina bagla
- [ ] Inbox/idempotency persistence ekle
- [ ] Transactional outbox'i MassTransit'in hazir pattern'ine tasima opsiyonunu, migration ve test tabani oturduktan sonra degerlendir

## Faz 5: Local Operations ve Observability

- [x] docker compose ile PostgreSQL, RabbitMQ ve Ollama altyapisini ekle
- [x] README'yi calistirma senaryolariyla doldur
- [x] Baseline observability ekle: correlation, trace/message kimlikleri ve background processing gorunurlugu
- [ ] Structured logging, merkezi loglama ve distributed tracing katmanini ekle
- [ ] Testcontainers tabanli integration test projesi ekle

## Faz 6: Ileri Seviye Veri Mimarisi

- [ ] Read model tarafini ayri projection store'a ayir
- [ ] Audit/event history buyurse Marten event store gecisini degerlendir
- [ ] Uzun sureli approval akislari icin saga/state machine ekle
