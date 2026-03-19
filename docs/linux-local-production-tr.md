# SourceEx Linux Local Production Simulasyonu Rehberi

Bu rehber, `SourceEx` projesini Linux calisan bir sanal makineye publish ederek production'a olabildigince benzeyen bir yerel kurulum yapmak icin hazirlandi. Buradaki hedef sadece uygulamayi calistirmak degil; publish alma, servisleri arka planda surdurme, Nginx ile reverse proxy kurma, environment variable ile konfigürasyon yonetme ve loglari production benzeri sekilde izleme pratigi kazanmaktir.

Bu doküman genel bir blog yazisi degildir. Dogrudan bu repo'nun bugunku yapisina bakilarak yazildi. Bu yüzden anlatim boyunca asagidaki gercek projeler referans alinmistir:

- `SourceEx.Identity.API`
- `SourceEx.API`
- `SourceEx.Worker.Notification`
- `SourceEx.Worker.Audit`
- `SourceEx.Worker.Policy`
- `docker-compose.yml` icindeki PostgreSQL, RabbitMQ ve Ollama altyapisi

Bugun SourceEx'te iki ayri web host vardir:

- `SourceEx.Identity.API`: login, parola dogrulama, refresh token, rol ve claim uretimi
- `SourceEx.API`: expense is akislari, business rule'lar, outbox ve event yayinlama

Bu ayrim deployment tarafinda da cok onemlidir. Nginx arkasinda tek bir IP veya domain gorsen bile arkada iki farkli Kestrel host'u calisacaktir.

## 1. Bu projeyi Linux ortamina publish etmek icin genel olarak hangi adimlari izlemem gerekir?

Bu repo icin en mantikli yuksek seviye akis su sekildedir:

1. Gelistirme makinesinde cozumun derlenebilir oldugundan emin ol.
2. Hangi host projelerin publish edilecegini netlestir.
3. Her host proje icin ayri `dotnet publish` al.
4. Publish ciktisini Linux VM icine kopyala.
5. Linux tarafinda .NET runtime, Nginx ve systemd ortamini hazirla.
6. PostgreSQL, RabbitMQ ve Ollama'yi `docker compose` ile ayaga kaldir.
7. `SourceEx.Identity.API` ve `SourceEx.API` icin systemd servisleri tanimla.
8. Worker servislerini ayri systemd servisleri olarak tanimla.
9. Nginx ile gelen istekleri dogru host'a yonlendir.
10. Health check, login, expense olusturma ve event akislarini test et.
11. `journalctl`, Nginx loglari ve container loglari ile dogrulama yap.

Bu projede publish edilecek uygulamalar sunlardir:

- `src/SourceEx.Identity.API`
- `src/SourceEx.API`
- `src/SourceEx.Worker.Notification`
- `src/SourceEx.Worker.Audit`
- `src/SourceEx.Worker.Policy`

Yani burada deployment, tek bir API publish etmekten ibaret degildir. Iki ayri web uygulamasi ve uc ayri background worker vardir.

## 2. Projenin turune gore publish sureci nasil olmali?

### `SourceEx.Identity.API`

[SourceEx.Identity.API.csproj](../src/SourceEx.Identity.API/SourceEx.Identity.API.csproj) `Microsoft.NET.Sdk.Web` kullaniyor. Bu bir ASP.NET Core Web API host'udur. Publish sonrasi Kestrel ile calisir ve dis dunyaya dogrudan degil, Nginx reverse proxy arkasindan sunulmasi daha dogrudur.

Bu servis:

- kullanici kaydeder
- login yapar
- password hash kontrol eder
- refresh token dondurur
- roller ve claim'ler ile JWT üretir

### `SourceEx.API`

[SourceEx.API.csproj](../src/SourceEx.API/SourceEx.API.csproj) da `Microsoft.NET.Sdk.Web` kullaniyor. Bu host expense sistemi icin asil business API'dir.

Bu servis:

- expense request alir
- MediatR tabanli application akisini calistirir
- domain kurallarini uygular
- veritabani yazimini ve outbox kaydini yapar

### Worker servisleri

Su projeler worker host'tur:

- [SourceEx.Worker.Notification.csproj](../src/SourceEx.Worker.Notification/SourceEx.Worker.Notification.csproj)
- [SourceEx.Worker.Audit.csproj](../src/SourceEx.Worker.Audit/SourceEx.Worker.Audit.csproj)
- [SourceEx.Worker.Policy.csproj](../src/SourceEx.Worker.Policy/SourceEx.Worker.Policy.csproj)

Bunlar web uygulamasi degildir. Nginx arkasina alinmazlar. RabbitMQ tuketirler ve arka planda surekli calisirlarsa anlamli olurlar. Bu nedenle Linux'ta en dogal calisma bicimleri `systemd` servisleridir.

Bu projeye ozel publish yaklasimi su olmali:

- `SourceEx.Identity.API`: `dotnet publish` + `systemd` + `Nginx`
- `SourceEx.API`: `dotnet publish` + `systemd` + `Nginx`
- Worker'lar: `dotnet publish` + `systemd`
- PostgreSQL, RabbitMQ, Ollama: `docker compose`

Bu hibrit model yerelde production'a oldukca benzer bir kurulum sunar.

## 3. Local Linux makinede production benzeri ortam kurmak icin hangi bilesenler gerekir?

### .NET Runtime / SDK

Tum .NET projeleri `net10.0` hedefliyor. Bu nedenle Linux VM icinde en azindan .NET 10 runtime ailesine ihtiyacin var.

Pratikte en kolay secenek:

- `.NET SDK 10` kurmak

Bu sayede hem uygulama calisir hem de `dotnet --info`, `dotnet ef`, `dotnet publish` gibi komutlari Linux uzerinde de kullanabilirsin.

Eger framework-dependent publish kullanacaksan gerekenler:

- `ASP.NET Core Runtime 10` for `SourceEx.Identity.API`
- `ASP.NET Core Runtime 10` for `SourceEx.API`
- `.NET Runtime 10` for worker projeleri

### Nginx

Nginx bu projede iki sebeple onemlidir:

- tek giris noktasi saglamak
- farkli Kestrel host'larina path tabanli reverse proxy yapmak

Yani istemci acisindan tek IP veya domain gorunur; ama Nginx arkada:

- `/api/v1.0/identity/*` isteklerini `SourceEx.Identity.API`'ye
- diger expense API isteklerini `SourceEx.API`'ye

yonlendirebilir.

### systemd

`systemd` sayesinde:

- servisler arka planda calisir
- VM yeniden baslayinca otomatik ayağa kalkar
- restart policy tanimlanabilir
- loglari `journalctl` ile izlenir

Bu repo'da web host ve worker'larin tamami icin systemd kullanmak mantiklidir.

### Environment variables

Bu repo'da `appsettings.Production.json` dosyalari yok. Bu nedenle production benzeri kurulumda config degerlerini environment file ile vermek en saglikli secenektir.

Ozellikle disari alinmasi gereken ayarlar:

- `ConnectionStrings__Database`
- `MessageBroker__Host`
- `MessageBroker__Port`
- `MessageBroker__VirtualHost`
- `MessageBroker__Username`
- `MessageBroker__Password`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`
- `Jwt__AccessTokenLifetimeMinutes`
- `Jwt__RefreshTokenLifetimeDays`
- `IdentitySeed__Enabled`
- `IdentitySeed__DemoPassword`
- `Ollama__BaseUrl`
- `Ollama__Model`

### Reverse proxy

Bu projede reverse proxy mantigi soyledir:

- Kestrel sadece `127.0.0.1` gibi loopback adreslerde dinler
- disariya sadece Nginx acilir
- SSL termination gerekiyorsa Nginx'te yapilir

Bu yaklasim, local production simulasyonunu daha guvenli ve daha gercekci yapar.

### Logging

Projede Serilog, Seq veya OpenTelemetry henuz yok. Bu nedenle su anki en dogal loglama yontemi:

- uygulama loglari `stdout/stderr`
- systemd altinda `journalctl`
- Nginx access/error log
- Docker container loglari

Yani simdilik "production benzeri logging" demek, merkezi bir log platformundan cok sistem servis loglarini disiplinli izlemek demektir.

### HTTPS ve sertifika konusu

Local Linux VM'de production benzeri HTTPS denemek istersen:

- self-signed sertifika
- `mkcert`

gibi araclar kullanabilirsin.

Ama bu repo'da bir eksik var: [Program.cs](../src/SourceEx.API/Program.cs) ve [Program.cs](../src/SourceEx.Identity.API/Program.cs) icinde `UseHttpsRedirection()` var, fakat `UseForwardedHeaders()` yok. Bu, Nginx HTTPS terminate ederken scheme algisinda sorun cikarabilir. Bu nedenle ilk kurulumda once HTTP reverse proxy ile ilerlemek daha risksizdir.

## 4. Projeyi publish etmeden once proje icinde kontrol etmem gereken dosya ve ayarlar neler?

### `appsettings.json`

Kontrol etmen gereken temel dosyalar:

- [src/SourceEx.Identity.API/appsettings.json](../src/SourceEx.Identity.API/appsettings.json)
- [src/SourceEx.API/appsettings.json](../src/SourceEx.API/appsettings.json)
- [src/SourceEx.Worker.Notification/appsettings.json](../src/SourceEx.Worker.Notification/appsettings.json)
- [src/SourceEx.Worker.Audit/appsettings.json](../src/SourceEx.Worker.Audit/appsettings.json)
- [src/SourceEx.Worker.Policy/appsettings.json](../src/SourceEx.Worker.Policy/appsettings.json)

Bu dosyalardaki ayarlar simdilik local varsayimlarla geliyor:

- PostgreSQL `localhost:5432`
- RabbitMQ `localhost:5672`
- Ollama `localhost:11434`
- JWT signing key repo icinde sabit

Linux VM icinde de Docker altyapisini ayni host'ta calistiracaksan `localhost` veya `127.0.0.1` halen kullanilabilir. Ama host'lari ayirirsan tum bu degerleri guncellemen gerekir.

### `appsettings.Production.json`

Repo'da su an `appsettings.Production.json` dosyalari yok. Bu dogrudan hata degil ama production hazirligi eksigidir. Iki yol vardir:

- Production ayarlarini `appsettings.Production.json` icine koymak
- Tum production degerlerini `systemd` environment file ile vermek

Bu proje icin ikinci yontem daha temizdir.

### `launchSettings.json`

Repo'da `launchSettings.json` yok. Bu production deployment acisindan problem degildir. Zaten o dosya esas olarak local IDE debug kolayligi icindir.

### `Program.cs` ve startup davranisi

[SourceEx.API/Program.cs](../src/SourceEx.API/Program.cs) tarafinda dikkat edilmesi gerekenler:

- `UseExceptionHandler()` var
- `UseHttpsRedirection()` var
- `UseAuthentication()` ve `UseAuthorization()` var
- `UseRateLimiter()` var
- health endpoint'leri var

[SourceEx.Identity.API/Program.cs](../src/SourceEx.Identity.API/Program.cs) tarafinda:

- `UseExceptionHandler()` var
- `UseHttpsRedirection()` var
- `UseAuthentication()` ve `UseAuthorization()` var
- startup sirasinda `IdentityDataSeeder` calisiyor
- health endpoint'leri var

Eksik olan nokta:

- her iki host'ta da `UseForwardedHeaders()` yok

Bu, Nginx reverse proxy ile HTTPS terminate edilen senaryoda akilda tutulmalidir.

### Kestrel ayarlari

Projede ozel Kestrel config'i yok:

- `ConfigureKestrel(...)` yok
- `Kestrel` section'i yok

Bu nedenle Linux tarafinda port binding'i environment variable ile sabitlemek en temiz yoldur:

```bash
ASPNETCORE_URLS=http://127.0.0.1:5005
```

Expense API icin.

```bash
ASPNETCORE_URLS=http://127.0.0.1:5006
```

Identity API icin.

### Port ayarlari

Yerel production simulasyonunda su dagilim temiz olur:

- `SourceEx.API`: `127.0.0.1:5005`
- `SourceEx.Identity.API`: `127.0.0.1:5006`
- PostgreSQL: `127.0.0.1:5432`
- RabbitMQ: `127.0.0.1:5672`
- RabbitMQ management UI: `127.0.0.1:15672`
- Ollama: `127.0.0.1:11434`
- Nginx: `80` ve gerekiyorsa `443`

### Connection string'ler

Bu projede iki ayri veritabani mantigi var:

- expense sistemi icin `sourceex`
- identity sistemi icin `sourceex_identity`

Expense API tarafinda [AddInfrastructure](../src/SourceEx.Infrastructure/DependencyInjection.cs) `ConnectionStrings:Database` olmadan baslamaz.

Identity API tarafinda [Program.cs](../src/SourceEx.Identity.API/Program.cs) icindeki `IdentityDbContext` de `ConnectionStrings:Database` ister.

### Dis servis bagimliliklari

Bu repo tek basina ayakta kalmaz. Su servisler gerekir:

- PostgreSQL
- RabbitMQ
- Ollama

Ek bir proje-ozel ayrinti:

- [deploy/postgres/init/01-create-sourceex-identity-db.sh](../deploy/postgres/init/01-create-sourceex-identity-db.sh) ilk PostgreSQL boot'unda `sourceex_identity` veritabanini olusturur

Ama eger Docker volume daha once olustuysa bu init script bir daha calismayabilir. O durumda `sourceex_identity` veritabanini elle olusturman gerekebilir.

## 5. Linux makinede deployment icin ornek bir klasor yapisi nasil olmali?

Bu repo icin onerilen klasor yapisi:

```text
/opt/sourceex/
├─ identity-api/
│  ├─ current/
│  └─ releases/
├─ api/
│  ├─ current/
│  └─ releases/
├─ workers/
│  ├─ notification/
│  │  ├─ current/
│  │  └─ releases/
│  ├─ audit/
│  │  ├─ current/
│  │  └─ releases/
│  └─ policy/
│     ├─ current/
│     └─ releases/
└─ client/
   └─ sourceex-web/

/etc/sourceex/
├─ sourceex-identity-api.env
├─ sourceex-api.env
├─ sourceex-worker-notification.env
├─ sourceex-worker-audit.env
└─ sourceex-worker-policy.env
```

Bu yapinin mantigi sudur:

- binary ve publish dosyalari `/opt` altinda
- environment dosyalari `/etc` altinda
- ileride rollback istersen `releases/` klasorleri ile surumlu dagitim yapabilirsin

## 6. `dotnet publish` komutu bu proje icin nasil calistirilmali?

Her host proje icin ayri publish alman gerekir.

### Identity API publish

```bash
dotnet publish src/SourceEx.Identity.API/SourceEx.Identity.API.csproj -c Release -o ./publish/identity-api
```

### Expense API publish

```bash
dotnet publish src/SourceEx.API/SourceEx.API.csproj -c Release -o ./publish/api
```

### Notification worker publish

```bash
dotnet publish src/SourceEx.Worker.Notification/SourceEx.Worker.Notification.csproj -c Release -o ./publish/worker-notification
```

### Audit worker publish

```bash
dotnet publish src/SourceEx.Worker.Audit/SourceEx.Worker.Audit.csproj -c Release -o ./publish/worker-audit
```

### Policy worker publish

```bash
dotnet publish src/SourceEx.Worker.Policy/SourceEx.Worker.Policy.csproj -c Release -o ./publish/worker-policy
```

Windows'tan Linux'a kopyalayacaksan runtime'i acikca belirtmek daha nettir:

```bash
dotnet publish src/SourceEx.Identity.API/SourceEx.Identity.API.csproj -c Release -r linux-x64 --self-contained false -o ./publish/identity-api
dotnet publish src/SourceEx.API/SourceEx.API.csproj -c Release -r linux-x64 --self-contained false -o ./publish/api
```

Self-contained kullanmak istersen runtime'i Linux'a kurmak zorunda kalmazsin, ama cikti boyutu buyur.

## 7. Publish ciktlarini Linux tarafinda nereye koymak mantikli olur?

Pratik dagilim:

- `/opt/sourceex/identity-api/current`
- `/opt/sourceex/api/current`
- `/opt/sourceex/workers/notification/current`
- `/opt/sourceex/workers/audit/current`
- `/opt/sourceex/workers/policy/current`

Ilk kurulumda basit klasor modeli yeterlidir. Ileride rollback ihtiyaci dogarsa `releases/current` symlink modeline gecebilirsin.

## 8. Nginx ile bu projeyi nasil ayaga kaldiririm?

### Reverse proxy mantigi

Bu projede Nginx'in gorevi sadece bir API'ye proxy yapmak degil, iki farkli API host'unu tek giris noktasi altinda birlestirmektir.

Mantik su sekildedir:

1. `SourceEx.Identity.API` `127.0.0.1:5006` uzerinde calisir.
2. `SourceEx.API` `127.0.0.1:5005` uzerinde calisir.
3. Nginx `80` veya `443` portunda dis istek alir.
4. `/api/v1.0/identity/` ile baslayan istekleri `5006`'ya gonderir.
5. Diger API isteklerini `5005`'e gonderir.

Bu sayede client tarafinda tek base address kullanmak cok daha kolay olur.

### Ornek Nginx config

```nginx
server {
    listen 80;
    server_name _;

    location /api/v1.0/identity/ {
        proxy_pass         http://127.0.0.1:5006;
        proxy_http_version 1.1;

        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location / {
        proxy_pass         http://127.0.0.1:5005;
        proxy_http_version 1.1;

        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Dosya konumu ornegi:

```text
/etc/nginx/sites-available/sourceex
```

Etkinlestirme:

```bash
sudo ln -s /etc/nginx/sites-available/sourceex /etc/nginx/sites-enabled/sourceex
sudo nginx -t
sudo systemctl reload nginx
```

### Domain yoksa IP ile local test nasil yapilir?

Linux VM IP adresin `192.168.1.50` ise:

```bash
curl http://192.168.1.50/health/live
```

Login testi:

```bash
curl -X POST http://192.168.1.50/api/v1.0/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userNameOrEmail":"manager-001","password":"Passw0rd!"}'
```

Yani domain olmadan da production benzeri reverse proxy akisini test edebilirsin.

### HTTPS kullanacaksan

HTTPS tarafina gececeksen once `UseForwardedHeaders()` eksigini not et. Mevcut halde ilk guvenli adim HTTP reverse proxy ile baslamaktir. HTTPS'ye gecmeden once proxy header destegi eklemek daha dogrudur.

## 9. Uygulamayi arka planda surekli calistirmak icin systemd servisi nasil yazilir?

### Identity API icin ornek service dosyasi

```ini
[Unit]
Description=SourceEx Identity API
After=network-online.target docker.service
Wants=network-online.target

[Service]
WorkingDirectory=/opt/sourceex/identity-api/current
ExecStart=/usr/bin/dotnet /opt/sourceex/identity-api/current/SourceEx.Identity.API.dll
Restart=always
RestartSec=5
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5006
EnvironmentFile=/etc/sourceex/sourceex-identity-api.env

[Install]
WantedBy=multi-user.target
```

### Expense API icin ornek service dosyasi

```ini
[Unit]
Description=SourceEx API
After=network-online.target docker.service
Wants=network-online.target

[Service]
WorkingDirectory=/opt/sourceex/api/current
ExecStart=/usr/bin/dotnet /opt/sourceex/api/current/SourceEx.API.dll
Restart=always
RestartSec=5
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5005
EnvironmentFile=/etc/sourceex/sourceex-api.env

[Install]
WantedBy=multi-user.target
```

### Notification worker service

```ini
[Unit]
Description=SourceEx Notification Worker
After=network-online.target docker.service
Wants=network-online.target

[Service]
WorkingDirectory=/opt/sourceex/workers/notification/current
ExecStart=/usr/bin/dotnet /opt/sourceex/workers/notification/current/SourceEx.Worker.Notification.dll
Restart=always
RestartSec=5
User=www-data
Environment=DOTNET_ENVIRONMENT=Production
EnvironmentFile=/etc/sourceex/sourceex-worker-notification.env

[Install]
WantedBy=multi-user.target
```

### Audit worker service

```ini
[Unit]
Description=SourceEx Audit Worker
After=network-online.target docker.service
Wants=network-online.target

[Service]
WorkingDirectory=/opt/sourceex/workers/audit/current
ExecStart=/usr/bin/dotnet /opt/sourceex/workers/audit/current/SourceEx.Worker.Audit.dll
Restart=always
RestartSec=5
User=www-data
Environment=DOTNET_ENVIRONMENT=Production
EnvironmentFile=/etc/sourceex/sourceex-worker-audit.env

[Install]
WantedBy=multi-user.target
```

### Policy worker service

```ini
[Unit]
Description=SourceEx Policy Worker
After=network-online.target docker.service
Wants=network-online.target

[Service]
WorkingDirectory=/opt/sourceex/workers/policy/current
ExecStart=/usr/bin/dotnet /opt/sourceex/workers/policy/current/SourceEx.Worker.Policy.dll
Restart=always
RestartSec=5
User=www-data
Environment=DOTNET_ENVIRONMENT=Production
EnvironmentFile=/etc/sourceex/sourceex-worker-policy.env

[Install]
WantedBy=multi-user.target
```

### systemd komut akisi

```bash
sudo systemctl daemon-reload
sudo systemctl enable sourceex-identity-api
sudo systemctl enable sourceex-api
sudo systemctl enable sourceex-worker-notification
sudo systemctl enable sourceex-worker-audit
sudo systemctl enable sourceex-worker-policy
```

Baslat:

```bash
sudo systemctl start sourceex-identity-api
sudo systemctl start sourceex-api
sudo systemctl start sourceex-worker-notification
sudo systemctl start sourceex-worker-audit
sudo systemctl start sourceex-worker-policy
```

Durum kontrolu:

```bash
sudo systemctl status sourceex-identity-api
sudo systemctl status sourceex-api
sudo systemctl status sourceex-worker-notification
sudo systemctl status sourceex-worker-audit
sudo systemctl status sourceex-worker-policy
```

Log izleme:

```bash
journalctl -u sourceex-identity-api -f
journalctl -u sourceex-api -f
journalctl -u sourceex-worker-policy -f
```

## 10. Production benzeri test yaparken dikkat etmem gereken farklar neler?

### Development ve Production ortam farki

Production benzeri ortamda:

- daha az detayli hata cevabi gorursun
- portlar sabit olur
- servisler terminal yerine systemd altinda kosar
- config dosyalari yerine env file daha kritik hale gelir
- Nginx araya girdigi icin request zinciri degisir

### Environment name

Web host'lar icin:

```bash
ASPNETCORE_ENVIRONMENT=Production
```

Worker'lar icin:

```bash
DOTNET_ENVIRONMENT=Production
```

### Hata sayfalari / exception davranisi

Hem identity API hem expense API `UseExceptionHandler()` kullaniyor. Yani development exception page beklememelisin. Hata analizinde sunlara bakarsin:

- HTTP response
- `journalctl`
- Nginx error log

### Logging farklari

Yerelde terminale bakan geliştirici ile production benzeri kurulumdaki bakis acisi farklidir. Burada asagidakiler senin ana gozlem araclarin olur:

- `journalctl`
- Nginx access/error log
- Docker container loglari
- RabbitMQ management UI

### Static files

Expense API bir frontend host'u degildir. `UseStaticFiles()` yok. Angular client'i ayni Linux VM'de servis etmek istersen Nginx ile ayri statik yayin stratejisi kurman gerekir.

### CORS

API tarafinda belirgin bir CORS configurasyonu yok. Bu nedenle en temiz local production simülasyonu:

- client'i de Nginx ile ayni origin altinda servis etmek

veya

- testleri Postman/curl ile yapmak

### Proxy headers

Bu projede en onemli deployment eksiklerinden biri budur. `UseForwardedHeaders()` olmadigi icin ozellikle HTTPS ve gercek istemci IP'si gibi konularda dikkatli olman gerekir.

## 11. Bu projeyi local production simulasyonunda calistirirken hangi problemlerle karsilasabilirim?

### Port cakismasi

Belirti:

- API bind edemez
- Nginx baslamaz
- Docker container portu acar gibi yapip duser

Kontrol:

```bash
ss -tulpn | grep 5005
ss -tulpn | grep 5006
ss -tulpn | grep 80
ss -tulpn | grep 5432
ss -tulpn | grep 5672
```

### Izin problemleri

Belirti:

- systemd servis baslar gibi yapar ama kapanir
- publish klasorune erisemez
- env dosyasini okuyamaz

Kontrol et:

- `WorkingDirectory` dogru mu
- `ExecStart` dogru mu
- `www-data` veya sectigin kullanici dosyalari okuyabiliyor mu

### Nginx `502 Bad Gateway`

En sik hata senaryolarindan biridir. Genelde sunu anlatir:

- Nginx arkadaki host'a ulasamiyor
- host ayakta degil
- yanlis porta proxy var

Kontrol sirasini soyle kur:

1. `curl http://127.0.0.1:5006/health/live`
2. `curl http://127.0.0.1:5005/health/live`
3. `sudo nginx -t`
4. `journalctl -u sourceex-identity-api -f`
5. `journalctl -u sourceex-api -f`

### Yanlis publish

Belirti:

- `ExecStart` dosyayi bulamaz
- Linux'ta uyumsuz publish ciktilari
- eksik runtime hedefi

Windows'tan publish aliyorsan `-r linux-x64` vermek daha güvenli olur.

### Eksik runtime

Belirti:

- `It was not possible to find any compatible framework version`
- .NET host baslamaz

Kontrol:

```bash
dotnet --info
```

### Yanlis environment variable

Belirti:

- DB connection string bulunamaz
- JWT ayarlari eksik gelir
- RabbitMQ host yanlis olur
- identity seed davranisi bekledigin gibi calismaz

Ozellikle iki API oldugu icin yanlis env dosyasini yanlis servise vermemeye dikkat et.

### Database erisim problemi

Belirti:

- `/health/ready` donmez
- login calismaz
- expense kaydi patlar

Kontrol:

```bash
docker compose ps
docker logs sourceex-postgres
```

Gerekirse PostgreSQL'e girip hem `sourceex` hem `sourceex_identity` veritabanlarini kontrol et.

### RabbitMQ erisim problemi

Belirti:

- worker'lar baglanamaz
- publish veya consume akisi bozulur

Kontrol:

```bash
docker logs sourceex-rabbitmq
```

UI:

```text
http://<vm-ip>:15672
```

### Ollama erisim problemi

Belirti:

- policy worker warning loglari yazar
- AI analiz yerine fallback karar verir

Bu projede iyi taraf su: Ollama erisilemese bile policy worker tamamen cokmeyebilir.

## 12. Bu proje icin en mantikli deployment akisini oner

Bu repo'nun bugunku durumuna gore en mantikli deployment akisi su olur:

### Adim 1: Gelistirme makinesinde build kontrolu yap

```bash
dotnet build SourceEx.slnx
```

Not: Bu repo'da EF migration dosyalari yok. Hem identity hem expense veritabani tarafinda ilk olusum `EnsureCreated` ile bootstrap ediliyor. Bu, local production simulasyonu icin kabul edilebilir; ama gercek production'a gecmeden once migration stratejisi eklenmelidir.

### Adim 2: Tum host'lar icin publish al

```bash
dotnet publish src/SourceEx.Identity.API/SourceEx.Identity.API.csproj -c Release -r linux-x64 --self-contained false -o ./publish/identity-api
dotnet publish src/SourceEx.API/SourceEx.API.csproj -c Release -r linux-x64 --self-contained false -o ./publish/api
dotnet publish src/SourceEx.Worker.Notification/SourceEx.Worker.Notification.csproj -c Release -r linux-x64 --self-contained false -o ./publish/worker-notification
dotnet publish src/SourceEx.Worker.Audit/SourceEx.Worker.Audit.csproj -c Release -r linux-x64 --self-contained false -o ./publish/worker-audit
dotnet publish src/SourceEx.Worker.Policy/SourceEx.Worker.Policy.csproj -c Release -r linux-x64 --self-contained false -o ./publish/worker-policy
```

### Adim 3: Linux VM'de altyapiyi hazirla

Kur:

- .NET 10 SDK veya gerekli runtime'lar
- Nginx
- Docker ve Docker Compose

### Adim 4: Infra container'larini ayaga kaldir

```bash
docker compose up -d
```

Bu asamada:

- PostgreSQL
- RabbitMQ
- Ollama

ayaga kalkar.

### Adim 5: Publish ciktisini Linux'a kopyala

```bash
scp -r ./publish/identity-api user@linux-vm:/opt/sourceex/identity-api/current
scp -r ./publish/api user@linux-vm:/opt/sourceex/api/current
scp -r ./publish/worker-notification user@linux-vm:/opt/sourceex/workers/notification/current
scp -r ./publish/worker-audit user@linux-vm:/opt/sourceex/workers/audit/current
scp -r ./publish/worker-policy user@linux-vm:/opt/sourceex/workers/policy/current
```

### Adim 6: Environment file'lari olustur

Identity API icin:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5006
ConnectionStrings__Database=Host=127.0.0.1;Port=5432;Database=sourceex_identity;Username=postgres;Password=postgres
Jwt__Issuer=SourceEx.Local
Jwt__Audience=SourceEx.Client
Jwt__SigningKey=change-this-in-your-linux-vm-to-a-strong-secret-key
Jwt__AccessTokenLifetimeMinutes=120
Jwt__RefreshTokenLifetimeDays=7
IdentitySeed__Enabled=true
IdentitySeed__DemoPassword=Passw0rd!
```

Expense API icin:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5005
ConnectionStrings__Database=Host=127.0.0.1;Port=5432;Database=sourceex;Username=postgres;Password=postgres
MessageBroker__Host=127.0.0.1
MessageBroker__Port=5672
MessageBroker__VirtualHost=/
MessageBroker__Username=guest
MessageBroker__Password=guest
Jwt__Issuer=SourceEx.Local
Jwt__Audience=SourceEx.Client
Jwt__SigningKey=change-this-in-your-linux-vm-to-a-strong-secret-key
Jwt__AccessTokenLifetimeMinutes=120
```

Policy worker icin:

```dotenv
DOTNET_ENVIRONMENT=Production
MessageBroker__Host=127.0.0.1
MessageBroker__Port=5672
MessageBroker__VirtualHost=/
MessageBroker__Username=guest
MessageBroker__Password=guest
Ollama__Enabled=true
Ollama__BaseUrl=http://127.0.0.1:11434
Ollama__Model=gemma3
Ollama__TimeoutSeconds=60
```

### Adim 7: systemd servislerini yaz ve etkinlestir

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now sourceex-identity-api
sudo systemctl enable --now sourceex-api
sudo systemctl enable --now sourceex-worker-notification
sudo systemctl enable --now sourceex-worker-audit
sudo systemctl enable --now sourceex-worker-policy
```

### Adim 8: Nginx'i bagla

Nginx config'inde:

- `/api/v1.0/identity/` -> `127.0.0.1:5006`
- diger API path'leri -> `127.0.0.1:5005`

olarak yonlendirme yap.

### Adim 9: Test et

Sirayla su testi yap:

```bash
curl http://127.0.0.1:5006/health/live
curl http://127.0.0.1:5005/health/live
curl -X POST http://<vm-ip>/api/v1.0/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userNameOrEmail":"manager-001","password":"Passw0rd!"}'
```

Sonra donen access token ile expense endpoint'lerini cagir.

### Adim 10: Loglari kontrol et

```bash
journalctl -u sourceex-identity-api -f
journalctl -u sourceex-api -f
journalctl -u sourceex-worker-notification -f
journalctl -u sourceex-worker-audit -f
journalctl -u sourceex-worker-policy -f
sudo nginx -t
docker compose ps
```

## Bu proje su an production deployment icin tamamen hazir mi?

Hayir. Local production simulasyonu icin iyi bir temel var ama gercek production seviyesinde eksikler de var.

Eksik veya gelistirilmeye acik alanlar:

- `appsettings.Production.json` dosyalari yok
- EF migration dosyalari yok
- `UseForwardedHeaders()` yok
- merkezi loglama yok
- secrets yonetimi basit duzeyde
- identity tarafi icin gelismis hesap guvenligi ozellikleri yok
- worker saglik izleme mekanizmasi henuz temel seviyede

Ama tum bunlara ragmen bu repo, Linux VM + Nginx + systemd + Docker Compose ile production'a yakin bir prova yapmak icin yeterli bir taban sunuyor.

## Son onerim

Bu proje icin en mantikli ilk local production simulasyonu modeli su olsun:

- Nginx host uzerinde
- `SourceEx.Identity.API` ve `SourceEx.API` host uzerinde systemd ile
- worker'lar host uzerinde systemd ile
- PostgreSQL, RabbitMQ ve Ollama Docker Compose ile
- tum hassas ayarlar environment file ile

Bu modeli oturttuktan sonra ikinci asamada su iyilestirmelere gecebilirsin:

- `UseForwardedHeaders()` eklemek
- HTTPS'i yerlestirmek
- EF migration stratejisi getirmek
- Serilog/Seq veya OpenTelemetry eklemek
- CI/CD publish pipeline yazmak
