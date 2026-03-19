# SourceEx Roadmap

Bu roadmap, mevcut Clean Architecture iskeletini bozmeden projeyi uretime daha yakin hale getirmek icin secilen uygulama planidir.

## Faz 1: Security Baseline

- [x] API'ye JWT bearer authentication ekle
- [x] Authorization policy'leri tanimla
- [x] Local/development token uretebilen bir auth endpoint'i ekle
- [x] Expense endpoint'lerini token claim'leriyle calisir hale getir

## Faz 2: AI Policy Workflow

- [x] Ollama entegrasyonu icin ayri bir integration kutuphanesi ekle
- [x] Refit tabanli HTTP client tanimla
- [x] HTTP resilience ekle
- [x] ExpenseCreated event'ini tuketip risk degerlendirmesi yapan policy worker ekle
- [x] AI sonucunu yeni bir integration event olarak sisteme geri yayinla

## Faz 3: Messaging Hardening

- [x] AI sonuc event'ini audit ve notification worker'larina bagla
- [ ] Inbox/idempotency persistence ekle
- [ ] Transactional outbox'i MassTransit'in hazir pattern'ine tasima opsiyonunu degerlendir

## Faz 4: Local Operations

- [x] docker compose ile PostgreSQL, RabbitMQ ve Ollama altyapisini ekle
- [x] README'yi calistirma senaryolariyla doldur
- [ ] Testcontainers tabanli integration test projesi ekle

## Faz 5: Ileri Seviye Veri Mimarisi

- [ ] Read model tarafini ayri projection store'a ayir
- [ ] Audit/event history buyurse Marten event store gecisini degerlendir
- [ ] Uzun sureli approval akislari icin saga/state machine ekle
