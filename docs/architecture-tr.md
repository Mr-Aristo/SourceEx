# SourceEx Mimari Rehberi

Bu doküman, `SourceEx` projesini baştan sona anlamak isteyen bir öğrenciye, yeni katılan geliştiriciye veya projeyi devralacak bir ekibe rehber olması için yazıldı. Amaç sadece "hangi klasörde ne var" demek değil; sistemin neden bu şekilde tasarlandığını, katmanların birbirine nasıl bağlandığını, hangi desenlerin gerçekten kullanıldığını ve hangi noktaların hâlâ gelişmeye açık olduğunu net biçimde anlatmaktır.

Bu metin özellikle mevcut koda bakılarak yazılmıştır. Yani burada anlatılanlar genel bir Clean Architecture blog yazısı değil, doğrudan bu repo içindeki dosya ve sınıfların mimari yorumudur.

## 1. Giriş

### Projenin genel amacı ne?

`SourceEx`, harcama talebi (expense request) oluşturma, görüntüleme ve onaylama akışını yöneten bir .NET çözümüdür. Sistemin ana işi şudur:

1. Bir kullanıcı harcama oluşturur.
2. Sistem bu harcamayı veritabanına kaydeder.
3. Aynı işlem içinde bir olay (event) outbox tablosuna yazılır.
4. Arka planda bu olay RabbitMQ üzerinden yayınlanır.
5. Diğer servisler bu olayı dinleyerek audit, notification veya risk değerlendirmesi gibi işleri yapar.

Bu nedenle proje sadece bir CRUD API değildir. Temel senaryo küçük görünse de amaç, senkron API akışı ile asenkron event-driven akışı birlikte gösterebilmektir.

### Bu proje hangi problemi çözüyor?

Bu proje iki farklı problemi aynı anda çözmeyi hedefliyor:

- İş kuralı olan bir alanı (`Expense`) katmanlı ve yönetilebilir biçimde modellemek
- Yan etkileri doğrudan API içinde yapmak yerine event-driven bir yapıyla ayırmak

Örneğin harcama oluşturulduğu anda "audit log yaz", "risk analizi yap", "gerekirse bildirim üret" gibi görevler doğrudan HTTP isteğinin içine gömülmemiş. Bunun yerine sistem önce kendi temel işini yapıyor, sonra bunu event olarak dışarı taşıyor.

### Genel mimari yaklaşım ne?

Projede üç yaklaşım birleşiyor:

- Clean Architecture
- DDD (Domain-Driven Design) taktik desenleri
- CQRS + event-driven entegrasyon

Burada önemli bir dürüstlük notu var: Bu proje tam anlamıyla "her bounded context ayrı mikroservis" şeklinde bir sistem değil. Daha doğru tanım şudur:

**Katmanlı bir yazma tarafı (write side) + event-driven worker’lar + ayrı çalışan destek servisleri**

Yani mimari, mikroservis yönüne açık bir temel kuruyor; fakat mevcut kod hâlâ tek ana veritabanı etrafında dönen bir uygulama çekirdeğine sahip.

Burada bir ikinci dürüstlük notu daha önemli: bu dokümanda "hedeflenen mimari" ile "repo’da bugün gerçekten olan durum" aynı şey gibi anlatılmamalıdır. Bugünkü repo’da gerçekten olanlar:

- expense tarafında katmanlı yapı, CQRS, validation pipeline ve outbox akışı
- ayrı bir `SourceEx.Identity.API` host’u
- RabbitMQ tüketen worker’lar
- Ollama entegrasyonu
- initial EF migration dosyaları ve startup sırasında `MigrateAsync()` kullanımı
- proxy-aware host temelini kuran `UseForwardedHeaders()` desteği
- domain ve application tarafında başlangıç seviyesinde unit test projeleri
- correlation ID ve message/correlation loglaması ile basic observability foundation

Henüz tam olmayan veya future work olarak görülmesi gerekenler ise şunlar:

- daha kapsamlı migration governance ve release disiplini
- ileri seviye proxy / network hardening
- daha kapsamlı integration test coverage
- gelişmiş observability
- inbox / idempotency
- identity modülünün ana çözümdeki katmanlı mimariyle tam hizalanması

### Clean Architecture, DDD ve diğer desenler nasıl konumlanıyor?

- `Domain`, iş kurallarının merkezi
- `Application`, use case katmanı
- `Infrastructure`, veritabanı ve outbox gibi teknik ayrıntılar
- `API`, HTTP giriş noktası
- `Workers`, asenkron tüketiciler
- `BuildingBlocks`, tekrar kullanılabilir ortak altyapı
- `Contracts`, servisler arası mesaj sözleşmeleri

DDD burada özellikle `Expense` aggregate root’u, `Money` ve `ExpenseId` value object’leri ve domain event’lerle kendini gösteriyor. CQRS ise `CreateExpenseCommand`, `ApproveExpenseCommand`, `GetExpenseByIdQuery` gibi request tipleri ve handler’lar üzerinden uygulanmış. Clean Architecture tarafı ise bağımlılık yönünün içeriye doğru akması ve katmanların sorumluluk bazlı ayrılmasıyla kurulmuş.

## 2. Proje Yapısına Genel Bakış

Repo’nun önemli parçaları kabaca şöyle:

```text
.
├─ SourceEx.slnx
├─ docker-compose.yml
├─ src/
│  ├─ BuildingBlocks/
│  ├─ SourceEx.Domain/
│  ├─ SourceEx.Application/
│  ├─ SourceEx.Infrastructure/
│  ├─ SourceEx.Contracts/
│  ├─ SourceEx.API/
│  ├─ SourceEx.Identity.API/
│  ├─ SourceEx.Worker.Notification/
│  ├─ SourceEx.Worker.Audit/
│  ├─ SourceEx.Worker.Policy/
│  └─ SourceEx.Integrations.Ollama/
├─ client/
│  └─ sourceex-web/
└─ docs/
```

Burada mimariyi anlamak için esas alınması gereken solution dosyası `SourceEx.slnx` dosyasıdır. Bu dosya güncel proje listesini içeriyor.

### `src/BuildingBlocks`

Bu proje ortak teknik soyutlamaları içeriyor. En önemli iki alanı var:

- `CQRS/`: `ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`
- `Messaging/`: `IntegrationEvent` ve `AddMessageBroker(...)`

Bu katman neden var? Çünkü CQRS ve broker kayıt mantığını her projede tekrar tekrar yazmak istemiyoruz. Özellikle worker projelerinde `AddMessageBroker` ile assembly taratıp consumer’ları otomatik kaydetmek bu tekrarları azaltıyor.

### `src/SourceEx.Domain`

Bu proje sistemin çekirdeği. Burada:

- entity / aggregate sınıfları
- value object’ler
- enum’lar
- domain exception
- domain event’ler

yer alıyor.

Merkez sınıf [Expense](../src/SourceEx.Domain/Entities/Expense.cs). Bu sınıf iş kurallarını taşıyan aggregate root olarak düşünülmüş.

### `src/SourceEx.Application`

Use case’lerin yaşadığı katman burası. İçinde:

- command ve query tanımları
- handler’lar
- FluentValidation validator’ları
- MediatR pipeline behavior
- `IApplicationDbContext` gibi application-level abstraction’lar

bulunuyor.

Bu katmanın görevi iş kuralını yeniden yazmak değil, iş akışını yönetmektir. Domain nesnelerini kullanır, ama veritabanı ayrıntılarını bilmez.

### `src/SourceEx.Infrastructure`

Bu katman teknik detayları taşır:

- EF Core `DbContext`
- entity mapping’leri
- outbox tablosu
- outbox processor
- health check

Buradaki en kritik sınıf [ApplicationDbContext](../src/SourceEx.Infrastructure/Data/Context/ApplicationDbContext.cs). Çünkü hem veritabanı erişimini yapıyor hem de domain event’leri integration event’e çevirip outbox’a yazıyor.

### `src/SourceEx.Contracts`

Bu proje servisler arası mesaj sözleşmelerini içeriyor. Yani RabbitMQ üzerinden dolaşan event tipleri burada:

- `ExpenseCreatedIntegrationEvent`
- `ExpenseApprovedIntegrationEvent`
- `ExpenseRejectedIntegrationEvent`
- `ExpenseRiskAssessedIntegrationEvent`

Bu ayırım önemlidir. Domain event ile integration event aynı şey değildir. Domain event iç dünyaya, integration event ise süreçler arası iletişime aittir.

### `src/SourceEx.API`

HTTP giriş noktası burasıdır. Minimal API kullanılmış. İçinde:

- endpoint tanımları
- auth ve security kodu
- rate limiting
- global exception handling
- request/response contract’ları
- `Program.cs`

bulunur.

Bu proje mümkün olduğunca ince tutulmaya çalışılmıştır. Endpoint’ler doğrudan handler mantığı yazmaz; komut veya sorgu üretir ve MediatR’a gönderir.

### `src/SourceEx.Identity.API`

Bu proje ayrı kimlik modülüdür. Sistemde kullanıcı adı, parola, rol ve refresh token yönetimi artık burada durur. İçinde:

- kullanıcı ve rol entity’leri
- refresh token saklama modeli
- login / register / refresh / logout endpoint’leri
- JWT üretimi
- seeded local kullanıcılar

yer alır.

Bu tasarım önemlidir çünkü `SourceEx.API` artık parola doğrulayan servis değil, sadece kimliği doğrulayan token’ı tüketen business servis konumundadır.

Ama burada önemli bir mimari nüans var: `SourceEx.Identity.API` şu an bilinçli olarak pragmatik tutulmuş bir modüldür. Yani ayrı bir servis sınırı kurulmuş olsa da, iç yapıda expense tarafındaki gibi `Domain / Application / Infrastructure` ayrımı yok. Endpoint’ler doğrudan EF Core ve password hashing akışıyla çalışıyor. Bu kötü bir çözüm değildir; ama mevcut yapı "ayrılmış identity boundary" ile "tam katmanlı identity çözümü" arasında bir noktadadır.

### `src/SourceEx.Worker.Notification`

Bu worker event dinler ve şu an log üzerinden bildirim davranışını simüle eder. Gerçek e-posta entegrasyonu yoktur. Yani bu katman şu an "notification workflow’unun kabuğu" durumundadır.

### `src/SourceEx.Worker.Audit`

Bu worker audit amaçlı event’leri tüketir. Şu an fiziksel bir audit tablosuna yazmak yerine log atıyor. Bu, mimari iskeleti göstermek için yeterli; ama production seviyesinde kalıcı audit depolaması gerekirdi.

### `src/SourceEx.Worker.Policy`

Bu worker yeni oluşturulan harcamaları dinler ve risk değerlendirmesi yapar. Risk analizi doğrudan bu projede yazılmamış; [SourceEx.Integrations.Ollama](../src/SourceEx.Integrations.Ollama) projesine delege edilmiştir.

### `src/SourceEx.Integrations.Ollama`

Bu proje dış AI entegrasyonunu izole eder. İçinde:

- Refit istemcisi
- Ollama API sözleşmeleri
- konfigürasyon
- risk assessment servisi

bulunur.

Bu ayırım çok yerinde bir karar. Çünkü AI entegrasyonu domain’in bir parçası değildir; teknik bir entegrasyondur.

### `client/sourceex-web`

Bu, Angular tabanlı ayrı istemci kabuğudur. Özellikle bilinçli olarak `.NET` solution içine sokulmamış. Çünkü backend henüz test edilmeden frontend’i ana çözümle sıkı bağlamak istenmemiş.

İçinde:

- Angular standalone route yapısı
- auth ve expense API servisleri
- interceptor’lar
- facade’lar
- boş ama açıklamalı placeholder sayfalar

yer alır.

Bu çok önemli bir tasarım kararıdır: frontend henüz son ürün arayüzü değil, entegrasyon kabuğu olarak kurgulanmıştır.

## 3. Katmanlar ve Sorumluluklar

### Domain katmanı

Domain katmanının amacı, sistemin iş bilgisini taşımaktır. "Harcama nedir?", "Ne zaman onaylanabilir?", "Ne zaman reddedilebilir?" gibi soruların cevabı burada olmalıdır.

Bu katman:

- dış sistem bilmemeli
- veritabanı bilmemeli
- HTTP bilmemeli
- RabbitMQ bilmemeli

Bu projede bu hedef büyük ölçüde korunmuş. `SourceEx.Domain` projesinin kendi `.csproj` dosyasında başka proje referansı yok.

### Application katmanı

Application katmanının işi, use case’leri organize etmektir. Örneğin:

- harcama oluştur
- harcamayı onayla
- harcamayı getir

Bu katman domain’i kullanır, ama `DbContext`, HTTP veya broker ayrıntısını doğrudan bilmez. Bunun için arayüzler kullanır.

Bu projede `IApplicationDbContext` bu rolü üstleniyor.

### Infrastructure katmanı

Infrastructure katmanı "teknik olarak nasıl yapıyoruz?" sorusuna cevap verir:

- PostgreSQL’e nasıl bağlanıyoruz?
- EF Core mapping nasıl?
- Outbox tablosu nasıl tutuluyor?
- Mesajlar nasıl publish ediliyor?

Bu katman application’ın ihtiyaç duyduğu arayüzleri uygular.

### Presentation / API katmanı

API katmanı sistemin dışa açılan yüzüdür. İstekler burada başlar. Bu projede controller yerine minimal API tercih edilmiş. Yani endpoint tanımı doğrudan `MapPost`, `MapGet` biçiminde yapılıyor.

Bu katmanın sorumluluğu:

- request’i almak
- kimlik doğrulama ve yetki kontrolünü yapmak
- request’i application request’ine çevirmek
- sonucu HTTP response’a çevirmek

### Shared / Common katmanları

Bu projede iki ortak katman var:

- `BuildingBlocks`: uygulama genelinde tekrar kullanılabilir teknik soyutlamalar
- `SourceEx.Contracts`: süreçler arası veri sözleşmeleri

Bu ayrım öğretici açıdan çok değerli. Çünkü çoğu projede "shared" adı altında her şey tek pakete doldurulur. Burada ise CQRS/messaging altyapısı ile event contract’ları ayrı düşünülmüş.

## 4. Domain Katmanının Detaylı Analizi

### Domain modeli nasıl kurulmuş?

Domain’in merkezinde [Expense](../src/SourceEx.Domain/Entities/Expense.cs) sınıfı var. Bu sınıf `Aggregate<ExpenseId>` sınıfından türediği için aggregate root olarak konumlanmış.

`Expense` şu alanları taşır:

- `EmployeeId`
- `DepartmentId`
- `Amount`
- `Description`
- `Status`

Burada `Amount` basit bir `decimal` değil, [Money](../src/SourceEx.Domain/ValueObjects/Money.cs) value object’i. `Id` de çıplak `Guid` değil, [ExpenseId](../src/SourceEx.Domain/ValueObjects/ExpenseId.cs). Bu, DDD açısından olumlu bir tercih.

### Entity, Value Object, Aggregate

Bu projede:

- Entity: `Expense`
- Value Object: `Money`, `ExpenseId`
- Aggregate Root: `Expense`

`Expense` aggregate root olduğu için sistemde harcama ile ilgili durum değişikliği bunun üzerinden yapılır. Örneğin onaylama davranışı:

- `Approve(...)`
- `Reject()`

metotları aggregate içinde.

Bu önemli çünkü iş kuralları handler’a dağılmıyor. Handler sadece "git ve aggregate’e bu davranışı uygulat" diyor.

### Domain event yapısı

Domain event’ler [ExpenseEvents.cs](../src/SourceEx.Domain/Events/ExpenseEvents.cs) içinde tanımlı:

- `ExpenseCreatedDomainEvent`
- `ExpenseApprovedDomainEvent`
- `ExpenseRejectedDomainEvent`

Temel event sınıfı [DomainEvent](../src/SourceEx.Domain/Abstractions/DomainEvent.cs). Bu sınıf event kimliği ve zamanını sabit tutuyor:

- `EventId`
- `OccurredOnUtc`

Bu event’ler MediatR `INotification` olarak publish edilmiyor. Bunun yerine `ApplicationDbContext.SaveChangesAsync(...)` içinde yakalanıp integration event’e çevriliyor. Yani domain event mekanizması **in-process handler** için değil, **outbox üretmek** için kullanılıyor.

Bu tasarım farkını anlamak önemlidir.

### İş kuralları nerede tutuluyor?

İş kuralları büyük ölçüde `Expense` içinde:

- oluştururken zorunlu alan kontrolü
- onaylarken durumun `Pending` olması
- onaylayan departmanın harcamanın departmanı ile aynı olması
- reddederken yine sadece `Pending` olabilmesi

Bu, domain modelin tamamen anemic olmadığını gösteriyor.

### Rich domain model mi, anemic model mi?

Bu proje için en doğru ifade şudur:

**Küçük ama davranış içeren bir rich domain model var.**

Yani model tamamen anemic değil, çünkü:

- `Create(...)`, `Approve(...)`, `Reject()` davranışları domain içinde
- state transition domain’de
- domain event üretimi domain’de

Ama çok zengin de değil; çünkü:

- aggregate içinde alt entity veya child collection yok
- domain service yok
- specification yok
- approval dışında karmaşık kural seti yok

Dolayısıyla bu proje "DDD-lite" veya "taktik DDD başlangıcı" olarak okunmalı.

### Domain açısından güçlü yönler

- `ExpenseId` ile güçlü tipli kimlik kullanımı
- `Money` ile para değerinin primitive yerine value object olarak modellenmesi
- aggregate üzerinde davranış olması
- domain event üretiminin domain içinden yapılması
- `DomainException` ile iş kuralı ihlallerinin teknik hatalardan ayrılması

### Domain açısından zayıf yönler

- `Reject()` davranışı domain’de var ama application/API üzerinden kullanılmıyor. Yani modelin bir kısmı henüz dışarı açılmamış.
- `Expense` dosyası `Entities` klasöründe dururken namespace olarak `SourceEx.Domain.Models` kullanıyor. Bu küçük ama kafa karıştırıcı bir tutarsızlık.
- Approval sürecinde "kim hangi role sahip", "kaç seviye onay gerekir" gibi daha karmaşık politikalar domain’e henüz taşınmamış.

## 5. Application Katmanının Detaylı Analizi

### Use case’ler nasıl modellenmiş?

Application katmanında şu use case’ler var:

- `CreateExpenseCommand`
- `ApproveExpenseCommand`
- `GetExpenseByIdQuery`

Bu bize doğrudan CQRS ayrımını gösteriyor:

- **Command**: sistemin durumunu değiştirir
- **Query**: veri okur, durum değiştirmez

Burada CQRS fiziksel olarak iki ayrı veritabanı anlamında kullanılmıyor. Daha çok kod organizasyonu ve niyet netliği için kullanılıyor.

### Handler yapıları nasıl kurgulanmış?

Handler’lar `BuildingBlocks.CQRS.Handlers` altındaki soyutlamaları uygular:

- `CreateExpenseCommandHandler`
- `ApproveExpenseCommandHandler`
- `GetExpenseByIdQueryHandler`

Bu handler’lar çok önemli bir prensibi koruyor: iş kuralını handler’ın içine gömmüyorlar. Örneğin onay akışında handler, aggregate’i buluyor ve sadece:

`expense.Approve(request.ApproverId, request.ApproverDepartmentId);`

çağrısını yapıyor.

Bu sayede handler use case akışını yönetirken, domain davranışı yine domain’de kalıyor.

### DTO, request, response, validator kullanımı

Query tarafında `ExpenseDetailsResponse` var. Bu, doğrudan domain entity’sini API’ye döndürmek yerine okunacak bir response modeli üretildiğini gösteriyor.

Validation tarafında FluentValidation kullanılmış:

- `CreateExpenseCommandValidator`
- `ApproveExpenseCommandValidator`
- `GetExpenseByIdQueryValidator`

Validation davranışı tek tek endpoint içinde değil, [ValidationBehavior](../src/SourceEx.Application/Behaviors/ValidationBehavior.cs) üzerinden pipeline’da işletiliyor.

Bu çok kritik bir tercih. Çünkü aynı command ileride başka bir giriş noktası üzerinden de çalıştırılsa, validation yine uygulanabilir.

### Service abstraction nasıl kullanılmış?

Application katmanının persistence ile tek teması [IApplicationDbContext](../src/SourceEx.Application/Data/IApplicationDbContext.cs). Bu arayüzde üç operasyon var:

- `AddExpenseAsync`
- `GetExpenseByIdAsync`
- `SaveChangesAsync`

Bu, klasik generic repository pattern değil. Daha sade ve use-case odaklı bir persistence port.

### Application katmanının gerçek rolü nedir?

Bu katman:

- request’i alır
- validasyonu çalıştırır
- domain modelini kullanır
- persistence abstraction’ını çağırır
- sonucu döner

Yani "uygulama aklı" burada, ama "iş kuralının özü" domain’de.

### Bu katmanda olmayan ama teoride sık görülen şeyler

Bu projede şunlar yok:

- Result pattern
- Specification pattern
- ayrı application service sınıfları
- domain event handler’lar
- mapping library

Bunların olmaması yanlış değil. Hatta bu proje ölçeğinde sadelik sağlar.

## 6. Infrastructure Katmanının Detaylı Analizi

### Veritabanı erişimi nasıl yapılmış?

Veritabanı erişimi EF Core ile yapılmış. [DependencyInjection.cs](../src/SourceEx.Infrastructure/DependencyInjection.cs) içinde `ApplicationDbContext` PostgreSQL için `UseNpgsql(...)` ile kaydediliyor.

`ApplicationDbContext` şu tabloları yönetiyor:

- `Expenses`
- `OutboxMessages`

### ORM entegrasyonu nasıl uygulanmış?

Entity mapping’ler ayrı configuration sınıflarına bölünmüş:

- [ExpenseConfiguration](../src/SourceEx.Infrastructure/Data/Configuration/ExpenseConfiguration.cs)
- [OutboxMessageConfiguration](../src/SourceEx.Infrastructure/Data/Configuration/OutboxMessageConfiguration.cs)

Bu iyi bir tercih. Çünkü `OnModelCreating(...)` şişmiyor ve mapping kuralları merkezi ama ayrık duruyor.

Özellikle `ExpenseConfiguration` içinde:

- `ExpenseId` dönüşümü
- `Money` owned type olarak haritalama
- kolon uzunlukları
- enum dönüşümü
- indeks

tanımlanmış.

### Repository pattern uygulanmış mı?

Klasik anlamda `ExpenseRepository` gibi bir repository sınıfı yok. Bunun yerine `ApplicationDbContext`, `IApplicationDbContext` arayüzünü uygulayarak application’a servis veriyor.

Bu yüzden burada en doğru ifade:

**Repository pattern tam olarak uygulanmamış; onun yerine DbContext üzerinden ince bir application port yaklaşımı seçilmiş.**

Bu yaklaşım sade ve anlaşılır. Dezavantajı ise domain büyüdükçe sorgu çeşitliliğinin `DbContext` arayüzüne dağılabilmesidir.

### Unit of Work var mı?

Ayrı bir `UnitOfWork` sınıfı yok. Ama EF Core `DbContext` ve `SaveChangesAsync()` pratikte Unit of Work gibi davranıyor.

Özellikle burada `SaveChangesAsync()` override edildiği için tek transaction içinde şu iki iş yapılıyor:

1. aggregate değişiklikleri veritabanına hazırlanıyor
2. domain event’lerden outbox mesajları üretiliyor

Bu çok önemli. Çünkü outbox deseninin temel amacı zaten "veri kaydı oldu ama event publish olmadı" gibi tutarsızlıkları azaltmaktır.

### Outbox nasıl çalışıyor?

Sistemin en öğretici parçalarından biri burası.

[ApplicationDbContext](../src/SourceEx.Infrastructure/Data/Context/ApplicationDbContext.cs) `SaveChangesAsync()` içinde ChangeTracker üzerinden domain event taşıyan aggregate’leri buluyor. Sonra her domain event için `CreateOutboxMessage(...)` çağrılıyor.

Burada örneğin:

- `ExpenseCreatedDomainEvent` -> `ExpenseCreatedIntegrationEvent`
- `ExpenseApprovedDomainEvent` -> `ExpenseApprovedIntegrationEvent`

dönüşümü yapılıyor.

Sonuç olarak `OutboxMessages` tablosuna JSON içerikli bir kayıt yazılıyor.

Ardından [OutboxProcessor](../src/SourceEx.Infrastructure/Outbox/OutboxProcessor.cs) her 10 saniyede bir bekleyen kayıtları çekiyor ve `IPublishEndpoint` üzerinden publish ediyor.

Bu yaklaşımın çözdüğü problem:

- API içinde doğrudan broker’a publish edip sonra DB save’in patlaması
- ya da DB save olup broker publish’in patlaması

gibi veri tutarsızlığı risklerini azaltmaktır.

### External servisler ve entegrasyonlar nerede?

Burada önemli bir ayrım var:

- Veritabanı ve outbox altyapısı `SourceEx.Infrastructure`
- AI entegrasyonu ise `SourceEx.Integrations.Ollama`

Bu da makul bir tercih. Çünkü her teknik detay aynı projeye doldurulmamış.

### Bu katmanın zayıf yanları

- Outbox processor basit polling yapıyor; çok instance’lı dağıtık ortam için lock/idempotency stratejisi yok.
- Inbox pattern yok.
- Retry var ama dead-letter, poison message veya daha gelişmiş hata politikaları yok.
- Initial EF migration dosyaları artık repo içinde var ve host’lar startup sırasında `MigrateAsync()` ile şemayı uyguluyor.
- Buna rağmen migration tarafı henüz olgun bir release disiplini seviyesinde değil. Rollback, environment bazlı migration kontrolü ve sürümleme politikası future work olarak duruyor.

## 7. API / Sunum Katmanı

### İstek sisteme nasıl giriyor?

Tüm uygulama [Program.cs](../src/SourceEx.API/Program.cs) üzerinden ayağa kalkıyor. Burada:

- ProblemDetails
- exception handler
- HTTP logging
- OpenAPI
- JWT auth
- API versioning
- rate limiting
- application ve infrastructure servisleri
- message broker

kaydediliyor.

Controller kullanılmamış; bunun yerine endpoint’ler şu dosyalarda:

- [AuthEndpoints](../src/SourceEx.API/Endpoints/AuthEndpoints.cs)
- [ExpenseEndpoints](../src/SourceEx.API/Endpoints/ExpenseEndpoints.cs)

### Controller yerine minimal API neden seçilmiş olabilir?

Bu projede endpoint sayısı az ve akışlar oldukça net. Minimal API bu durumda şu avantajları getiriyor:

- daha az dosya seremonisi
- endpoint tanımı daha okunabilir
- request pipeline’a yakın çalışma

Özellikle bir öğretici projede, endpoint ile use case arasındaki ilişki daha görünür oluyor.

### Authentication / authorization nasıl bağlanmış?

`SourceEx.API/Security` klasörü expense servisinin JWT doğrulama tarafının merkezidir.

- `JwtOptions`: konfigürasyon modeli
- `ClaimsPrincipalExtensions`: claim okuma yardımcıları
- `AuthorizationPolicies`: policy adları
- `ServiceCollectionExtensions`: auth register işlemleri

Burada kritik nokta şu:

Bu projede artık kimlik üretimi expense API’nin içinde yapılmıyor. JWT üretimi [SourceEx.Identity.API](../src/SourceEx.Identity.API) içinde. Expense API sadece o token’ı doğruluyor ve claim’leri iş akışına taşıyor.

Yani kullanıcı girişleri `SourceEx.Identity.API` içindeki `/api/v1.0/identity/auth/*` endpoint’lerinden geçiyor. Expense API tarafındaki `/api/v1.0/auth/me` endpoint’i ise yalnızca mevcut token’ın claim bilgisini göstermeye yarıyor.

### Hata yönetimi nasıl çalışıyor?

[GlobalExceptionHandler](../src/SourceEx.API/ExceptionHandling/GlobalExceptionHandler.cs) tüm beklenmeyen exception’ları yakalıyor ve `ProblemDetails` standardına çeviriyor.

Örneğin:

- `RequestValidationException` -> `400`
- `DomainException` -> `400`
- `KeyNotFoundException` -> `404`
- `SecurityTokenException` -> `401`

Bu sayede application/domain exception’ları HTTP seviyesinde tutarlı cevaplara dönüşüyor.

### Response standardı nasıl kurgulanmış?

API şu anda karmaşık bir ortak response envelope kullanmıyor. Bunun yerine ASP.NET Core’un doğal biçimleri tercih edilmiş:

- başarılı durumda typed response
- hatalı durumda ProblemDetails / ValidationProblemDetails

Bu sadelik çoğu senaryo için yeterli.

### Bir HTTP isteğinin baştan sona yolculuğu

Örnek olarak `POST /api/v1.0/expenses` isteğini ele alalım:

1. İstek [ExpenseEndpoints](../src/SourceEx.API/Endpoints/ExpenseEndpoints.cs) içindeki `CreateExpenseAsync` metoduna gelir.
2. JWT’den `user_id` ve `department_id` claim’leri okunur.
3. `CreateExpenseRequest`, `CreateExpenseCommand` nesnesine dönüştürülür.
4. `ISender.Send(...)` ile MediatR pipeline’ına girer.
5. `ValidationBehavior` validator’ları çalıştırır.
6. `CreateExpenseCommandHandler` domain nesnelerini üretir.
7. `Expense.Create(...)` aggregate oluşturur ve domain event ekler.
8. `IApplicationDbContext.SaveChangesAsync()` çağrılır.
9. Infrastructure katmanı outbox kaydını yazar.
10. API `201 Created` döner.
11. Arka planda outbox processor mesajı publish eder.
12. Worker’lar bu olayı ayrı ayrı tüketir.

Bu akış, projenin senkron ve asenkron bölümlerini birlikte anlamak için en iyi örnektir.

## 8. Bağımlılık Akışı ve Clean Architecture Yorumu

### Katmanlar arası bağımlılık yönü nasıl?

Kabaca bağımlılık akışı şöyle:

```text
Domain
  ^
  |
Application
  ^
  |
Infrastructure

API ------> Application
API ------> Infrastructure
API ------> Contracts
API ------> BuildingBlocks

Workers --> Contracts
Workers --> BuildingBlocks
Policy Worker --> Integrations.Ollama
```

Burada okunması gereken temel fikir:

- iç katmanlar dış katmanları bilmez
- dış katmanlar iç katmanları kullanır

### Hangi katman hangisini referanslıyor?

Gerçek proje referanslarına bakınca:

- `SourceEx.Domain`: kimseye bağlı değil
- `SourceEx.Application`: `Domain` + `BuildingBlocks`
- `SourceEx.Infrastructure`: `Application` + `Domain` + `Contracts` + `BuildingBlocks`
- `SourceEx.API`: `Application` + `Infrastructure` + `Contracts` + `Domain` + `BuildingBlocks`
- Worker’lar: `Contracts` + `BuildingBlocks`, bazıları ek olarak `Integrations.Ollama`

Bu yapı büyük ölçüde Clean Architecture ruhuna uygun.

### Dependency inversion nasıl uygulanmış?

En temel örnek `IApplicationDbContext`.

Application katmanı "ben EF Core bilmek istemiyorum, bana harcamayı ekleyebileceğim ve kaydedebileceğim bir soyutlama ver" diyor. Infrastructure ise bunu `ApplicationDbContext` ile sağlıyor.

Bu yaklaşım sayesinde handler’lar persistence teknolojisine doğrudan bağımlı olmuyor.

### Arayüzler neden gerekli?

Arayüzler burada iki nedenle kullanılmış:

1. Katmanlar arası bağımlılık yönünü korumak
2. Uygulama niyetini teknoloji ayrıntısından ayırmak

Ancak dürüst olmak gerekirse, bu projede arayüz kullanımı aşırı değil. Sadece kritik yerde var. Bu iyi bir şey. Her sınıf için yapay interface üretilmemiş.

### Clean Architecture gerçekten doğru uygulanmış mı?

Genel değerlendirme: **Büyük ölçüde evet, ama bazı tartışmalı noktalar var.**

Güçlü taraflar:

- Domain katmanı temiz tutulmuş
- Validation application’a taşınmış
- API ince tutulmuş
- Infrastructure teknik ayrıntıları kapsıyor
- Worker’lar application/domain katmanlarını taşımıyor

Tartışmalı veya geliştirilebilir taraflar:

- Query tarafında ayrı bir read model yok; aynı write model okunuyor
- Repository pattern yok; bu kötü değil ama domain büyüdükçe sorgu/persistence arayüzü şişebilir
- Kimlik modülü ayrılsa da henüz external IAM, MFA, lockout, password reset gibi ileri auth özellikleri yok
- API’de `GetExpenseById` için sahiplik/department kontrolü yok; sadece authenticated user yeterli

Özetle, mimari doğru yönde. Ama hâlâ öğretici/prototip ile production arasında bir yerde duruyor.

## 9. Kullanılan Tasarım Desenleri ve Teknik Tercihler

### Clean Architecture

Bu desen katmanları sorumluluk bazlı ayırmak için kullanılmış. Nerede görüyoruz?

- `Domain`, `Application`, `Infrastructure`, `API`

Neden tercih edilmiş olabilir? Çünkü iş mantığını HTTP, EF veya broker gibi teknolojilerden ayırmak uzun vadede bakım kolaylığı sağlar.

Alternatif ne olabilirdi? Tek proje içinde controller-service-repository yaklaşımı.

### DDD Taktik Desenleri

Bu projede gerçekten kullanılan DDD öğeleri:

- Aggregate Root: `Expense`
- Value Object: `Money`, `ExpenseId`
- Domain Event: `ExpenseCreatedDomainEvent` vb.
- Domain Exception

Kullanılmayan ama teoride DDD’de sık görülen yapılar:

- Domain Service
- Specification
- bounded context ayrımı

Yani proje DDD’nin tamamını değil, işe yarayan çekirdek taktiklerini almış.

### CQRS

`ICommand`, `IQuery`, handler arayüzleri ve MediatR ile uygulanmış.

Bu projede CQRS ne kazandırıyor?

- use case’leri niyet bazlı ayırıyor
- create/approve/get akışlarını ayrı tutuyor
- validator ve handler yapılarını netleştiriyor

Alternatif ne olabilirdi? Tek bir service sınıfı içinde `Create`, `Approve`, `Get` metotları.

### MediatR

MediatR, request’lerin handler’lara yönlendirilmesini sağlıyor. Bu sayede endpoint’ler doğrudan handler sınıfı bilmeden `ISender` ile iş yapıyor.

Bu iyi mi? Küçük projelerde biraz soyutlama maliyeti yaratır. Ama bu projede CQRS + pipeline behavior ile birlikte kullanıldığı için mantıklı.

### Pipeline Behavior

[ValidationBehavior](../src/SourceEx.Application/Behaviors/ValidationBehavior.cs), cross-cutting concern olarak validation’ı merkezi hale getiriyor.

Bu önemli çünkü validation endpoint’in içinde değil, request pipeline’ında. Böylece use case seviyesi koruma sağlanıyor.

### Outbox Pattern

Bu projedeki en değerli pattern’lerden biri. Domain event -> integration event -> outbox -> broker zinciri ile güvenilir yayın akışı amaçlanıyor.

Alternatif ne olabilirdi? Handler içinde doğrudan RabbitMQ publish etmek. O yaklaşım daha basit ama daha risklidir.

### Background Service

`OutboxProcessor`, `BackgroundService` tabanlı. Sürekli çalışan küçük bir daemon gibi davranıyor. Bu, publish işini HTTP request ömründen ayırıyor.

### Consumer Pattern / Event-Driven Processing

Worker projelerindeki consumer’lar bu desenin örneği:

- `ExpenseApprovedIntegrationEventConsumer`
- `ExpenseCreatedIntegrationEventConsumer`
- `ExpenseRiskAssessedIntegrationEventConsumer`

Bu sayede yan etkiler birbirinden ayrılmış.

### Options Pattern

`JwtOptions` ve `OllamaOptions` ile konfigürasyon strongly typed hale getirilmiş. Bu, düz string okuma yerine daha güvenli ve okunabilir.

### Mapping

Bu projede mapping elle yapılıyor. Örneğin:

- query handler domain entity’yi `ExpenseDetailsResponse`’a dönüştürüyor
- API endpoint bunu `ExpenseResponse`’a dönüştürüyor
- Angular client tarafında `ExpenseResponse` -> `ExpenseDetailViewModel` dönüşümü var

Yani AutoMapper gibi bir kütüphane yok. Bu proje ölçeğinde bu, çoğu zaman daha okunabilir.

### Gereksiz pattern veya overengineering var mı?

Biraz dürüst konuşursak, bu proje çok küçük bir alan problemi için nispeten zengin bir mimari kullanıyor. Yani sadece "harcama oluştur/onayla" için:

- CQRS
- MediatR
- Outbox
- RabbitMQ
- Worker’lar
- AI integration

kullanılması bazı ekipler için fazla gelebilir.

Ama bu proje yalnızca iş çözmek için değil, mimari pratik göstermek için de tasarlanmış görünüyor. O açıdan bu tercihler anlamlı.

## 10. Veri Akışı Örneği

Burada en öğretici örnek `CreateExpense` akışı.

### Adım 1: HTTP request API’ye gelir

İstemci `POST /api/v1.0/expenses` çağrısı yapar. Request body şu alanları taşır:

- `amount`
- `currency`
- `description`

Kullanıcı kimliği request body’den değil, JWT claim’lerinden gelir. Bu önemli bir tasarım kararıdır.

### Adım 2: Endpoint request’i command’e çevirir

[ExpenseEndpoints](../src/SourceEx.API/Endpoints/ExpenseEndpoints.cs) içindeki `CreateExpenseAsync` metodu:

- request body’yi alır
- claim’leri okur
- `CreateExpenseCommand` üretir

Yani API katmanı burada adapter görevi görür.

### Adım 3: MediatR pipeline’ı devreye girer

`ISender.Send(...)` çağrısı yapıldığında önce [ValidationBehavior](../src/SourceEx.Application/Behaviors/ValidationBehavior.cs) devreye girer. `CreateExpenseCommandValidator` çalışır.

Eğer validasyon başarısızsa:

- `RequestValidationException` fırlatılır
- `GlobalExceptionHandler` bunu `400 ValidationProblemDetails` olarak döner

### Adım 4: Handler domain modelini kullanır

`CreateExpenseCommandHandler`:

- yeni `ExpenseId` üretir
- `Money.Of(...)` çağırır
- `Expense.Create(...)` ile aggregate oluşturur

Burada iş kuralı kontrolü domain içinde yapılır.

### Adım 5: Persistence ve outbox aynı akışta çalışır

Handler `AddExpenseAsync(...)` ve `SaveChangesAsync(...)` çağırır.

`SaveChangesAsync(...)` içinde:

- aggregate’in domain event’leri okunur
- bunlar integration event’e çevrilir
- `OutboxMessages` tablosuna yazılır
- sonra veriler birlikte commit edilir

Bu, yazma tarafının en kritik güvenilirlik mekanizmasıdır.

### Adım 6: HTTP cevabı döner

API `201 Created` döner ve `expenseId` verir.

Bu noktada kullanıcı açısından işlem bitmiş görünür. Ama arka planda event akışı devam eder.

### Adım 7: Outbox processor publish eder

`OutboxProcessor` bekleyen mesajı bulur, deserialize eder ve `IPublishEndpoint.Publish(...)` ile broker’a yollar.

### Adım 8: Worker’lar devreye girer

- `SourceEx.Worker.Audit`: oluşturma olayını loglar
- `SourceEx.Worker.Policy`: risk analizi yapar

Policy worker, [ExpenseCreatedIntegrationEventConsumer](../src/SourceEx.Worker.Policy/Consumers/ExpenseCreatedIntegrationEventConsumer.cs) içinde Ollama servisini çağırır.

### Adım 9: İkinci dalga event üretimi olur

Policy worker:

- risk sonucu üretir
- `ExpenseRiskAssessedIntegrationEvent` publish eder

Sonra:

- Audit worker bunu kaydeder
- Notification worker gerekirse manual review log’u üretir

Bu örnek, bu projede veri akışının sadece request-response olmadığını; olay tabanlı ikinci bir yaşam döngüsü olduğunu gösterir.

## 11. Konfigürasyon, Dependency Injection ve Başlangıç Akışı

### `Program.cs` ne yapıyor?

[Program.cs](../src/SourceEx.API/Program.cs) bu projenin composition root’u. Yani bütün bağımlılıklar burada birleştiriliyor.

Başlatma sırasında şunlar oluyor:

1. ProblemDetails ve global exception handler ekleniyor
2. HTTP logging ekleniyor
3. OpenAPI ekleniyor
4. JWT auth ve authorization policy’leri ekleniyor
5. API versioning konfigüre ediliyor
6. Rate limiting ekleniyor
7. Application katmanı servisleri ekleniyor
8. Infrastructure katmanı servisleri ekleniyor
9. Message broker ekleniyor

Sonra middleware pipeline kuruluyor:

- `UseExceptionHandler()`
- `UseHttpsRedirection()`
- `UseHttpLogging()`
- `UseAuthentication()`
- `UseAuthorization()`
- `UseRateLimiter()`

Burada daha önce eksik olan önemli nokta artık başlangıç seviyesinde kapatılmış durumda: hem expense API hem de identity API tarafında `UseForwardedHeaders()` var. Yani host’lar artık reverse proxy arkasında gelen `X-Forwarded-*` başlıklarını okuyabiliyor.

Ama bu işi tamamen bitmiş saymamak gerekir. Çünkü production sertliğinde bir kurulum için hâlâ:

- `KnownProxies` / `KnownNetworks` gibi güvenilir proxy tanımları
- ortam bazlı HTTPS termination doğrulaması
- gerçek istemci IP ve güven sınırı politikası

gibi ayarlar düşünülmelidir. Yani reverse proxy readiness artık "yok" değil; "temel seviyesi kurulmuş" durumdadır.

### Katmanlar DI ile nasıl bağlanmış?

#### Application

`AddApplication()` içinde:

- MediatR register edilir
- validator’lar register edilir
- validation behavior eklenir

#### Infrastructure

`AddInfrastructure()` içinde:

- `ApplicationDbContext`
- `IApplicationDbContext -> ApplicationDbContext`
- `OutboxProcessor`
- health checks

eklenir.

#### Messaging

`AddMessageBroker(...)` içinde MassTransit kuruluyor. Worker projeleri consumer assembly’lerini vererek kendi consumer’larını otomatik kaydettiriyor.

Bu yöntem, özellikle worker sayısı arttığında bakım kolaylığı sağlar.

### Worker’lar nasıl ayağa kalkıyor?

Her worker’ın kendi `Program.cs` dosyası var. Bunlar hafif host’lar:

- Notification worker sadece broker + consumer assembly
- Audit worker sadece broker + consumer assembly
- Policy worker broker + Ollama integration

Bu ayrım, her worker’ın sadece kendi ihtiyacı kadar bağımlılık almasını sağlıyor.

### Angular istemcide başlangıç akışı nasıl?

`client/sourceex-web` tarafında:

- `main.ts` ile `bootstrapApplication(...)`
- `app.config.ts` ile router ve HTTP client
- interceptor’larla auth token ve API version header

kuruluyor.

Bu client şimdilik ana sistemin parçası değil; entegrasyon kabuğu olarak düşünülmeli.

## 12. Hata Yönetimi, Validation ve Cross-Cutting Concerns

### Validation nerede ve nasıl yapılıyor?

Ana validation stratejisi application katmanında:

- FluentValidation
- MediatR pipeline behavior

Bu, Clean Architecture açısından doğru yönde bir tercih. Çünkü validation HTTP’ye bağlı kalmıyor.

Ancak önemli bir istisna var: ayrı kimlik modülündeki login/register endpoint’leri application katmanı üzerinden değil, doğrudan minimal API + EF yaklaşımıyla yazılmış durumda. Yani auth tarafında validation daha manuel ilerliyor.

Bu istisna kabul edilebilir; çünkü identity modülü burada önce işlevsel ayrışma amacıyla eklenmiş. Ama ileride mimariyi tamamen hizalamak istersen auth use case’leri için de `Application` benzeri ayrı bir katman kurulabilir.

Benzer şekilde identity tarafında güvenlik sertleştirmesi artık tamamen boş değil. Minimum uygulanabilir seviye olarak:

- login / register / refresh endpoint’lerinde rate limiting
- başarısız giriş sayısı takibi
- geçici account lockout
- parola politikası doğrulaması
- refresh token cleanup

repo’da uygulanmış durumda.

Buna rağmen bu alan bitmiş değildir. Brute-force telemetry, daha detaylı güvenlik denetimi, MFA, şüpheli oturum yönetimi ve daha kapsamlı audit hâlâ backlog konularıdır.

### Exception handling nasıl?

Global exception handling API’de merkezi olarak çözülmüş. Bu iyi çünkü:

- endpoint’ler `try/catch` ile dolmuyor
- hata-HTTP eşlemesi tek yerde tutuluyor
- ProblemDetails standardı kullanılıyor

### Logging nasıl?

Şu an logging temel host logging ve consumer logger’ları ile yapılıyor. API’de ayrıca `AddHttpLogging()` var. Bunun üstüne başlangıç seviyesinde observability desteği de eklenmiş:

- iki API host’unda correlation ID middleware var
- `ProblemDetails` cevaplarına correlation ID ekleniyor
- host logging tarafında activity tracking açık
- outbox publish logları `MessageId` ve `CorrelationId` taşıyor
- worker consumer logları message/correlation kimliklerini yazıyor

Ama dürüst olmak gerekirse gelişmiş observability henüz yok:

- Serilog yok
- Seq yok
- OpenTelemetry yok
- distributed tracing yok
- merkezi dashboard / metric katmanı yok

Bu yüzden sistem izlenebilirliği artık "tamamen temel" değil, ama hâlâ başlangıç seviyesinde.

### Transaction yönetimi nasıl?

Ayrı transaction behavior veya unit of work decorator yok. Bunun yerine EF Core `SaveChangesAsync()` ve outbox aynı akışta kullanılıyor.

Bu küçük proje için yeterli. Ama daha karmaşık use case’lerde transaction sınırları daha görünür hale getirilebilir.

### Authorization nasıl ele alınmış?

Authorization policy tabanlı:

- `AuthenticatedUser`
- `ExpenseApprover`

`Approve` endpoint’i rol ve claim ister. Ancak `GetExpenseById` sadece authenticated olmayı ister; sahiplik veya departman denetimi yoktur. Bu, demo için anlaşılır olsa da gerçek sistemde dikkat edilmesi gereken bir açık noktadır.

### Performance ve rate limiting

Rate limiting API’de aktif. Okuma ve yazma için ayrı politika var:

- read: fixed window
- write: sliding window

Bu olgun bir API davranışı göstergesi. Ayrıca partisyon anahtarı olarak authenticated user veya IP kullanılması doğru bir tercih.

### Caching var mı?

Hayır. Şu an hiçbir katmanda cache yok.

### Idempotency var mı?

Tam anlamıyla yok.

- API tarafında idempotency key yok
- worker tarafında inbox/deduplication yok
- outbox processor çoklu instance koordinasyonu yapmıyor

Yani mimari yön olarak doğru ama güvenilir mesaj işleme açısından henüz başlangıç seviyesinde.

### Result pattern var mı?

Yok. Bu projede hata akışı `Result<T>` yerine exception üzerinden yönetiliyor.

Bu kötü bir seçim değildir; sadece bilinçli bir tercih. Özellikle ASP.NET Core `ProblemDetails` ile birleşince oldukça okunabilir kalıyor.

## 13. Güçlü Yönler ve Geliştirilebilecek Alanlar

### Güçlü yönler

Bu projede gerçekten güçlü olan noktalar şunlar:

#### 1. Katman ayrımı net

Her şey tek projeye yığılmamış. Domain, application, infrastructure ve API sınırları okunabilir durumda.

#### 2. Domain davranışı domain’de

`Expense` sadece veri taşıyan bir DTO değil. Onay ve red kuralları aggregate üzerinde.

#### 3. Validation doğru yere taşınmış

FluentValidation + pipeline behavior uygulama katmanında. Bu oldukça iyi bir karar.

#### 4. Outbox yaklaşımı öğretici ve doğru

Event-driven mimariyi güvenilir biçimde kurmak için en doğru başlangıçlardan biri.

#### 5. Worker’lar application/domain’e bağımlı değil

Bu sayede asenkron süreçler gerçekten ayrışmış durumda.

#### 6. Ollama entegrasyonu izole

AI entegrasyonu çekirdek iş modeline bulaştırılmamış.

#### 7. Ayrı Angular istemci kararı doğru

Backend henüz olgunlaşmadan frontend’i ana çözümle sıkı bağlamamak sağlıklı bir karar.

### Geliştirilebilecek alanlar

#### 1. Test tabanı yeni başladı ama dar

Şu an repo’da artık `tests/SourceEx.Domain.Tests` ve `tests/SourceEx.Application.Tests` projeleri var. Bu iyi bir başlangıç; çünkü aggregate davranışı ve validator akışları en azından otomatik olarak doğrulanabiliyor.

Ama kapsam hâlâ dar. Handler testleri, API testleri, worker/infrastructure testleri ve Testcontainers tabanlı integration test’ler henüz yok. Yani sorun artık "hiç test yok" değil, "test temeli var ama genişletilmeli" seviyesinde.

#### 2. Migration stratejisi başladı ama olgun değil

EF Core migration dosyaları artık repo içinde mevcut ve startup tarafında `MigrateAsync()` kullanılıyor. Bu önceki `EnsureCreated()` yaklaşımına göre belirgin bir ilerleme.

Buna rağmen migration tarafı hâlâ başlangıç aşamasında. Release disiplini, rollback yaklaşımı ve environment bazlı migration yönetimi henüz güçlü şekilde tanımlanmış değil.

#### 3. Reject akışı eksik

Domain ve contract seviyesinde red desteği var, ama application ve API’de dışarı açılmamış.

#### 4. Read authorization eksik

`GetExpenseById` endpoint’i "bu kullanıcı bu kaydı görebilir mi?" sorusunu sormuyor.

#### 5. Idempotency ve inbox yok

Özellikle event-driven yapılarda bu eksik önemli hale gelir.

#### 6. Observability başlangıç seviyesinde

Correlation ID, activity tracking ve message/correlation loglaması eklendi. Bu çok faydalı bir temel. Ama merkezi loglama, tracing ve metric katmanı olmadığı için sistem davranışını üretim kalitesinde izlemek için daha fazlası gerekir.

#### 7. Reverse proxy readiness temel seviyede

Deployment dokümanları Nginx senaryosunu anlatıyor ve host uygulamalarda artık `UseForwardedHeaders()` da var. Buna rağmen güvenilir proxy tanımları ve production ağ topolojisine göre sertleştirme hâlâ eksik.

#### 8. Identity hardening başladı ama tamamlanmadı

Identity servisi artık login rate limiting, lockout, parola politikası ve refresh token cleanup gibi temel sertleştirmelere sahip. Ancak brute-force analitiği, MFA, daha kapsamlı güvenlik denetimi ve zengin operasyonel audit hâlâ yok.

#### 9. Identity modülü mimari olarak pragmatik

Identity servisi ayrı bir sınır olarak doğru yerde duruyor; ama iç tasarım henüz expense tarafındaki kadar katmanlı değil. Bu, gelecekte değerlendirilecek bilinçli bir trade-off.

#### 10. CQRS fiziksel ayrışmaya gitmemiş

Bu kötü değil, ama beklentiyi doğru koymak gerekir. Şu an read ve write aynı veri modelinden çalışıyor.

#### 11. Bazı isimlendirme ve yapı tutarsızlıkları var

Örneğin `Entities` klasöründeki `Expense` dosyasının namespace’inin `Models` olması gibi küçük tutarsızlıklar ileride bakım maliyeti yaratabilir.

### Bu projeden hangi dersler çıkarılmalı?

Bir öğrenci için bu proje şu dersi verir:

- Katman ayırmak tek başına amaç değildir; her katmanın sınırı ve nedeni olmalıdır.
- DDD demek her yere karmaşık soyutlama koymak değildir; birkaç doğru tactical pattern bile büyük fark yaratır.
- Event-driven yapı kurarken güvenilir yayın konusu göz ardı edilmemelidir; outbox burada kritik bir ders sunar.
- "Temiz mimari" gerçek dünyada her zaman tam kusursuz olmaz; önemli olan güçlü sınırlar kurup eksikleri dürüstçe görmektir.

## 14. Sonuç

Bu proje bir öğrenciye sadece `.NET API nasıl yazılır` konusunu öğretmez. Aynı zamanda şu kavramları pratikte gösterir:

- katmanlı mimari
- Clean Architecture düşüncesi
- DDD’nin temel taktik desenleri
- CQRS
- MediatR
- FluentValidation pipeline
- EF Core ile persistence
- transactional outbox
- RabbitMQ / MassTransit ile event-driven işlem
- background worker yapısı
- JWT tabanlı auth iskeleti
- dış AI entegrasyonunu çekirdekten ayırma yaklaşımı

Bu projeyi anlayan biri şunu kavramış olur:

Bir sistem sadece endpoint yazmaktan ibaret değildir. İyi tasarlanmış bir sistemde veri, davranış, iş akışı, teknik detaylar ve dış entegrasyonlar farklı katmanlarda ele alınır. Amaç karmaşa üretmek değil; değişimi kontrol edilebilir hale getirmektir.

`SourceEx` bu yolun tamamlanmış son hali değil, ama doğru kavramları aynı repo içinde bir araya getiren güçlü bir öğretici örnektir. En değerli tarafı da budur: hem iyi kararları hem de henüz tamamlanmamış alanları görünür kılması.
