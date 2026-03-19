# SourceEx Linux Local Production Simulasyonu Rehberi

Bu rehber, `SourceEx` projesini Linux çalışan bir sanal makineye publish ederek, gerçek production ortamına mümkün olduğunca benzeyen bir akış kurmak için yazıldı. Amaç sadece uygulamayı "çalıştırmak" değil; **publish, servis olarak ayağa kaldırma, Nginx arkasına alma, ortam değişkenleriyle yönetme ve log kontrolü yapma** gibi production benzeri adımları yerelde tekrarlayabilmektir.

Bu doküman doğrudan mevcut repo yapısına bakılarak hazırlanmıştır. Yani anlatım genel değil, bu projedeki gerçek dosyalara dayalıdır.

## 1. Bu projeyi Linux ortamına publish etmek için genel olarak hangi adımları izlemem gerekir?

SourceEx için en mantıklı yüksek seviye akış şöyledir:

1. Windows veya geliştirme makinesinde projeyi derlenebilir hale getir.
2. Veritabanı migration meselesini çöz.
3. API ve worker projeleri için ayrı ayrı `dotnet publish` al.
4. Publish çıktılarını Linux VM içine kopyala.
5. Linux üzerinde gerekli runtime ve altyapıları hazırla.
6. PostgreSQL, RabbitMQ ve Ollama’yı ayağa kaldır.
7. API için Nginx reverse proxy kur.
8. API ve worker’ları `systemd` servisleri olarak tanımla.
9. Environment variable ve connection string’leri Linux tarafında ayarla.
10. Health check, log ve uçtan uca testlerle doğrula.

Bu projede tek bir yayınlanabilir uygulama yok. En azından şu host’lar ayrı ayrı düşünülmeli:

- `SourceEx.API`
- `SourceEx.Worker.Notification`
- `SourceEx.Worker.Audit`
- `SourceEx.Worker.Policy`

Yani publish süreci bir web API publish’inden ibaret değil; aynı zamanda üç ayrı worker servisini de kapsıyor.

## 2. Projenin türüne göre publish süreci nasıl olmalı?

### `SourceEx.API`

Bu proje [SourceEx.API.csproj](../src/SourceEx.API/SourceEx.API.csproj) dosyasından görüldüğü üzere `Microsoft.NET.Sdk.Web` kullanıyor. Yani bu bir **ASP.NET Core Web API** uygulamasıdır.

Bu nedenle:

- publish sonrası bir `dll` ve host dosyaları oluşur
- arka planda Kestrel ile çalışır
- dış dünyaya doğrudan değil, çoğunlukla Nginx reverse proxy arkasından açılması tavsiye edilir

### Worker projeleri

Şu projeler `Microsoft.NET.Sdk.Worker` kullanıyor:

- [SourceEx.Worker.Notification.csproj](../src/SourceEx.Worker.Notification/SourceEx.Worker.Notification.csproj)
- [SourceEx.Worker.Audit.csproj](../src/SourceEx.Worker.Audit/SourceEx.Worker.Audit.csproj)
- [SourceEx.Worker.Policy.csproj](../src/SourceEx.Worker.Policy/SourceEx.Worker.Policy.csproj)

Bunlar web uygulaması değildir. Nginx arkasına konulmazlar. Bunlar:

- arka planda sürekli çalışan process’lerdir
- RabbitMQ’ya bağlanır
- event tüketir
- log üretir

Bu yüzden en doğal Linux çalıştırma biçimleri `systemd` servisidir.

### Bu proje için publish yaklaşımı

Bu repo için en mantıklı yaklaşım şudur:

- API: `dotnet publish` + `systemd` + `Nginx`
- Worker’lar: `dotnet publish` + `systemd`
- PostgreSQL / RabbitMQ / Ollama: mevcut [docker-compose.yml](../docker-compose.yml) ile container içinde

Bu hibrit model yerelde production’a oldukça benzer bir deneyim verir:

- altyapı servisleri container’da
- uygulama servisleri host üzerinde systemd ile
- giriş noktası Nginx

Bu repo şu an Dockerfile içermediği için, ilk aşamada tüm .NET host’ları container’a almak yerine bu model daha uygulanabilir.

## 3. Local Linux makinede production benzeri ortam kurmak için hangi bileşenler gerekir?

### .NET Runtime / SDK

Bu çözüm `net10.0` hedefliyor. Bunu `.csproj` dosyalarında açıkça görüyoruz.

Linux VM’de iki farklı yaklaşım var:

#### Seçenek A: Framework-dependent publish

Bu yaklaşımda Linux makinede runtime kurulu olur, publish çıktısı daha küçük olur.

Gerekli olan:

- API için `ASP.NET Core Runtime 10`
- Worker’lar için `.NET Runtime 10`

Pratikte en kolay yol genelde `.NET SDK 10` kurmaktır. Özellikle sunucuda `dotnet --info`, `dotnet ef` veya hızlı teşhis yapacaksan SDK işi kolaylaştırır.

#### Seçenek B: Self-contained publish

Bu yaklaşımda Linux makinede runtime kurman gerekmez. Ama çıktı boyutu artar.

Yerel production simülasyonu için iki yaklaşım da olur. Ben bu proje özelinde ilk aşamada **framework-dependent publish + runtime kurulu Linux** öneririm. Çünkü:

- log ve komut teşhisi daha kolaydır
- publish çıktısı küçüktür
- birden fazla host projeyi yönetmek daha pratiktir

### Nginx

Nginx bu projede iki iş için değerlidir:

- dış istekleri `SourceEx.API`’ye reverse proxy ile yönlendirmek
- istersen Angular client build çıktısını statik olarak servis etmek

Worker’lar Nginx arkasına konmaz.

### systemd

`systemd`, API ve worker süreçlerini servis olarak yönetmek için gerekir. Bunun avantajı:

- arka planda çalışırlar
- sunucu yeniden başlasa otomatik ayağa kalkarlar
- logları `journalctl` ile izlenebilir
- restart politikası verilebilir

### environment variables

Bu projede production benzeri kurulum için environment variable kullanmak çok mantıklıdır. Çünkü repo içinde şu anda:

- `appsettings.Production.json` yok
- sırları ve adresleri `appsettings.json` içinde bırakmak iyi bir production alışkanlığı değil

Özellikle şu ayarlar dışarı alınmalıdır:

- `ConnectionStrings__Database`
- `MessageBroker__Host`
- `MessageBroker__Port`
- `MessageBroker__Username`
- `MessageBroker__Password`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`
- `Ollama__BaseUrl`
- `Ollama__Model`

### Reverse proxy

API doğrudan internet yüzüne açılmak yerine Nginx arkasında çalışmalıdır. Nginx:

- 80/443 portlarını dinler
- gelen isteği Kestrel’e yollar
- SSL termination yapabilir
- tek giriş noktası sağlar

### Logging

Bu projede şu an dosya tabanlı özel bir logging çözümü yok. Serilog/Seq/OpenTelemetry henüz yok.

Dolayısıyla Linux local production simülasyonunda en gerçekçi başlangıç:

- uygulama loglarını `stdout/stderr` ile üretmek
- `systemd` altında `journalctl` ile izlemek

Bu repo’nun mevcut durumuna en uygun yaklaşım budur.

### HTTPS ve sertifika konusu

Eğer local Linux VM’de production’a benzer HTTPS testi yapmak istiyorsan iki yol var:

- self-signed sertifika
- `mkcert` gibi araçla local trusted sertifika

Ama bu projede şu an ters proxy arkasında doğru forwarded headers desteği kodda eklenmemiş. Bu önemli bir ayrıntı. Eğer Nginx HTTPS terminate edecekse, uygulamanın reverse proxy arkasında çalıştığını doğru anlaması gerekir.

Mevcut kodda [Program.cs](../src/SourceEx.API/Program.cs) içinde:

- `UseHttpsRedirection()` var
- ama `UseForwardedHeaders()` yok

Bu nedenle HTTPS ve proxy birlikte kullanılınca redirect davranışında sorun yaşanabilir. Bu, production hazırlığı açısından şu anki eksiklerden biridir.

## 4. Projeyi publish etmeden önce proje içinde kontrol etmem gereken dosya ve ayarlar neler?

Bu bölüm çok önemlidir. Çünkü publish sorunu çoğu zaman `dotnet publish` komutundan değil, yanlış yapılandırmadan çıkar.

### `appsettings.json`

Şu an şu dosyalar var:

- [src/SourceEx.API/appsettings.json](../src/SourceEx.API/appsettings.json)
- [src/SourceEx.Worker.Notification/appsettings.json](../src/SourceEx.Worker.Notification/appsettings.json)
- [src/SourceEx.Worker.Audit/appsettings.json](../src/SourceEx.Worker.Audit/appsettings.json)
- [src/SourceEx.Worker.Policy/appsettings.json](../src/SourceEx.Worker.Policy/appsettings.json)

Buradaki mevcut değerler tamamen local geliştirme odaklı:

- DB: `localhost:5432`
- RabbitMQ: `localhost:5672`
- Ollama: `localhost:11434`
- JWT signing key: sabit ve repo içinde

Linux VM’ye geçerken bunlar gözden geçirilmelidir. Eğer altyapıyı aynı VM içinde container olarak çalıştıracaksan `localhost` hâlâ iş görebilir. Ama başka makineler veya özel bridge ağları kullanacaksan bu ayarlar değişir.

### `appsettings.Production.json`

Repo içinde şu an **hiçbir `appsettings.Production.json` dosyası yok**.

Bu bir eksik değil ama production benzeri ortam için hazırlık eksikliğidir. İki yaklaşım var:

#### Yaklaşım 1

`appsettings.Production.json` dosyaları oluşturursun.

#### Yaklaşım 2

Tüm production değerlerini `systemd` environment file üzerinden verirsin.

Bu proje için ikinci yaklaşım daha temiz olabilir. Çünkü sırlar dosyaya commit edilmez.

### `launchSettings.json`

Repo’da `launchSettings.json` bulunmuyor. Bu kötü bir şey değil. Production deployment için zaten kritik dosya değildir. Visual Studio veya local debug için daha anlamlıdır.

### `Program.cs`

[Program.cs](../src/SourceEx.API/Program.cs) incelendiğinde production hazırlığı açısından şunlar görülüyor:

- `UseExceptionHandler()` var
- `UseHttpsRedirection()` var
- `UseAuthentication()` / `UseAuthorization()` var
- `UseRateLimiter()` var
- health endpoint’leri var

Ama şu noktalar eksik:

- `UseForwardedHeaders()` yok
- HSTS yok
- özel Kestrel binding ayarı yok

Bu, Nginx arkasında HTTPS terminate edilen bir senaryoda dikkat gerektirir.

### Kestrel ayarları

Projede özel Kestrel konfigürasyonu bulunmuyor. Yani:

- `appsettings.json` içinde Kestrel section yok
- `Program.cs` içinde `ConfigureKestrel(...)` yok

Bu durumda Kestrel binding’i environment variable ile vermek en temiz çözümdür:

```bash
ASPNETCORE_URLS=http://127.0.0.1:5005
```

Ben local production simülasyonunda API’yi sadece loopback üzerinde dinletecek şekilde açmanı öneririm. Böylece dış dünyaya sadece Nginx açık olur.

### Port ayarları

Şu an projede sabit production port tanımı yok. Geliştirme notlarında `http://localhost:5000` örneklenmiş, ama bu runtime garantisi değildir.

Bu yüzden Linux tarafında portları açıkça belirlemek gerekir:

- API Kestrel: örneğin `127.0.0.1:5005`
- PostgreSQL: container içinde `5432`
- RabbitMQ: container içinde `5672`
- Ollama: container içinde `11434`
- Nginx: `80` veya `443`

### Connection string’ler

API için veritabanı zorunludur. [AddInfrastructure](../src/SourceEx.Infrastructure/DependencyInjection.cs) içinde `ConnectionStrings:Database` boşsa uygulama hiç başlamaz.

Bu yüzden API servisi ayağa kalkmıyorsa ilk bakılacak yerlerden biri budur.

### Dış servis bağımlılıkları

Bu proje tek başına başlamaz. Şunlara bağımlıdır:

- PostgreSQL
- RabbitMQ
- Ollama

Ayrıca migration dosyaları repo içinde olmadığı için veritabanı da publish öncesi hazır sayılmaz. Bu kritik nokta yerel production simülasyonunda gözden kaçabilir.

## 5. Linux makinede deployment için örnek klasör yapısı nasıl olmalı?

Bu proje için önerdiğim klasör yapısı:

```text
/opt/sourceex/
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
├─ sourceex-api.env
├─ sourceex-worker-notification.env
├─ sourceex-worker-audit.env
└─ sourceex-worker-policy.env
```

Bu yapının mantığı:

- uygulama binary’leri `/opt` altında
- environment ve secret benzeri ayarlar `/etc` altında
- istersen her publish’i `releases/2026-03-19_1200` gibi versiyonlu klasöre koyup `current` symlink’i ile yönetebilirsin

İlk kurulum için symlink şart değil. Sade kurulum da olur:

```text
/opt/sourceex/api/
/opt/sourceex/workers/notification/
/opt/sourceex/workers/audit/
/opt/sourceex/workers/policy/
```

## 6. `dotnet publish` komutu bu proje için nasıl çalıştırılmalı?

Bu projede her host proje için ayrı publish alman gerekir.

### API publish

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

### Linux runtime belirterek publish

Windows’tan Linux VM’ye taşıyacaksan şu yaklaşım daha net olabilir:

```bash
dotnet publish src/SourceEx.API/SourceEx.API.csproj -c Release -r linux-x64 --self-contained false -o ./publish/api
```

Aynı mantığı worker’lara da uygulayabilirsin.

Bu komutta:

- `-r linux-x64`: Linux hedefini belirtir
- `--self-contained false`: runtime Linux tarafında kurulu olacak demektir

Eğer self-contained istersen:

```bash
dotnet publish src/SourceEx.API/SourceEx.API.csproj -c Release -r linux-x64 --self-contained true -o ./publish/api
```

Ama bu durumda çıktı büyür.

## 7. Publish çıktılarını Linux tarafında nereye koymak mantıklı olur?

Bu proje için pratik öneri:

- API: `/opt/sourceex/api/current`
- Notification worker: `/opt/sourceex/workers/notification/current`
- Audit worker: `/opt/sourceex/workers/audit/current`
- Policy worker: `/opt/sourceex/workers/policy/current`

Eğer basit kalmak istiyorsan:

- `/opt/sourceex/api`
- `/opt/sourceex/workers/notification`
- `/opt/sourceex/workers/audit`
- `/opt/sourceex/workers/policy`

Ben ilk aşamada basit klasör yapısını öneririm. Rollback ihtiyacı doğduğunda `releases/current` modeline geçersin.

## 8. Nginx ile bu projeyi nasıl ayağa kaldırırım?

### Reverse proxy mantığı

`SourceEx.API` doğrudan 80 veya 443 portunu dinlemek zorunda değildir. Hatta production benzeri yaklaşımda genelde dinlememelidir.

Mantık şudur:

1. `SourceEx.API` Kestrel ile örneğin `127.0.0.1:5005` üzerinde çalışır.
2. Nginx dışarıdan `80` veya `443` portunda istek alır.
3. Nginx isteği Kestrel’e iletir.
4. Kullanıcı sadece Nginx’i görür.

### Örnek Nginx config

Domain yoksa IP ile de çalıştırabilirsin. Basit bir config:

```nginx
server {
    listen 80;
    server_name _;

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

Bu dosyayı örneğin şuraya koyabilirsin:

```text
/etc/nginx/sites-available/sourceex
```

Sonra:

```bash
sudo ln -s /etc/nginx/sites-available/sourceex /etc/nginx/sites-enabled/sourceex
sudo nginx -t
sudo systemctl reload nginx
```

### Domain yoksa IP ile local test nasıl yapılır?

Eğer Linux VM IP adresin `192.168.1.50` ise:

```bash
curl http://192.168.1.50/health/live
```

veya tarayıcıdan:

```text
http://192.168.1.50
```

Eğer yalnızca API test edeceksen:

```bash
curl http://192.168.1.50/api/v1.0/auth/token
```

### HTTPS kullanacaksan

Örnek TLS config eklenebilir. Ama önemli uyarı:

Mevcut kodda `UseForwardedHeaders()` olmadığı için Nginx üzerinde HTTPS terminate edip Kestrel’e HTTP ile devam ettiğinde yönlendirme veya scheme algısında sorun çıkabilir. Bu nedenle şu anki haliyle en güvenli yerel simülasyon:

- önce HTTP reverse proxy ile test et
- sonra HTTPS gerekiyorsa uygulamaya forwarded header desteği ekle

## 9. Uygulamayı arka planda sürekli çalıştırmak için systemd servisi nasıl yazılır?

### API için örnek service dosyası

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

Dosya adı örneği:

```text
/etc/systemd/system/sourceex-api.service
```

### Notification worker için örnek service

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

### Audit worker için örnek service

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

### Policy worker için örnek service

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

### systemd komut akışı

Servis dosyalarını yazdıktan sonra:

```bash
sudo systemctl daemon-reload
sudo systemctl enable sourceex-api
sudo systemctl enable sourceex-worker-notification
sudo systemctl enable sourceex-worker-audit
sudo systemctl enable sourceex-worker-policy
```

Başlat:

```bash
sudo systemctl start sourceex-api
sudo systemctl start sourceex-worker-notification
sudo systemctl start sourceex-worker-audit
sudo systemctl start sourceex-worker-policy
```

Durum kontrolü:

```bash
sudo systemctl status sourceex-api
sudo systemctl status sourceex-worker-notification
sudo systemctl status sourceex-worker-audit
sudo systemctl status sourceex-worker-policy
```

Restart:

```bash
sudo systemctl restart sourceex-api
```

Log izleme:

```bash
journalctl -u sourceex-api -f
journalctl -u sourceex-worker-policy -f
```

## 10. Production benzeri test yaparken dikkat etmem gereken farklar neler?

### Development ve Production ortam farkı

Yerel geliştirmede birçok şey otomatik veya toleranslı çalışır. Production benzeri ortamda ise:

- exception detayları dışarı verilmez
- portlar sabitlenir
- servisler arka planda çalışır
- config dosyasına değil environment variable’a güvenilir
- reverse proxy devrededir

### Environment name

API için:

```bash
ASPNETCORE_ENVIRONMENT=Production
```

Worker’lar için:

```bash
DOTNET_ENVIRONMENT=Production
```

Bu ayrımı yapmak iyi olur. Her ikisini de set etmek istersen sorun değil, ama asıl host tipine göre kullanmak daha temizdir.

### Hata sayfaları / exception davranışı

Bu API zaten `UseExceptionHandler()` kullanıyor. Yani production tarafında geliştirici hata sayfası beklememelisin. Hata alırsan:

- HTTP response
- `journalctl`
- Nginx error log

üzerinden teşhis yaparsın.

### Logging farkları

Development’te terminalde gördüğün loglar production benzeri kurulumda:

- `journalctl`
- Nginx access/error log
- RabbitMQ arayüzü

üzerinden takip edilir.

### Static files

API içinde `UseStaticFiles()` yok. Zaten bu servis bir frontend host’u değil. Angular client’ı production benzeri testte Nginx ile ayrıca servis etmek istersen, o ayrı bir karardır.

### CORS

Eğer client ve API aynı origin üzerinden Nginx ile sunulursa CORS ihtiyacı azalır. Ama farklı portlardan çalıştırırsan CORS konusu gündeme gelir.

Şu an API’de açık bir CORS konfigürasyonu görünmüyor. Bu yüzden production benzeri yaklaşımda client’ı da Nginx arkasından aynı origin’e almak daha az sorun çıkarır.

### Proxy headers

Bu projede şu an kritik eksiklerden biri budur. Nginx arkasında doğru proxy header işlemek için uygulamaya forwarded headers desteği eklemek gerekir.

Mevcut durumda:

- `UseHttpsRedirection()` var
- `UseForwardedHeaders()` yok

Bu yüzden özellikle HTTPS senaryosunda dikkatli olmalısın.

## 11. Bu projeyi local production simülasyonunda çalıştırırken hangi problemlerle karşılaşabilirim?

### Port çakışması

Belirti:

- Nginx açılmaz
- API bind edemez
- Docker container başlayamaz

Kontrol:

```bash
ss -tulpn | grep 5005
ss -tulpn | grep 80
ss -tulpn | grep 5432
ss -tulpn | grep 5672
```

### İzin problemleri

Belirti:

- `systemd` servis başlar gibi yapar ama düşer
- publish klasörüne erişemez
- env dosyasını okuyamaz

Çözüm:

- `WorkingDirectory` doğru mu?
- `ExecStart` doğru mu?
- servis kullanıcısının klasör erişimi var mı?

### Nginx `502 Bad Gateway`

Bu en sık görülen sorundur. Genelde şu anlama gelir:

- Nginx arkadaki API’ye ulaşamıyor
- API hiç başlamamış
- yanlış porta proxy yapılıyor

Kontrol sırası:

1. API servisi ayakta mı?
2. `curl http://127.0.0.1:5005/health/live` API host içinde dönüyor mu?
3. Nginx config’te `proxy_pass` doğru mu?
4. `journalctl -u sourceex-api -f` ne diyor?

### Yanlış publish

Belirti:

- `ExecStart` dosyayı bulamaz
- yanlış runtime hedefi
- eksik dosya

Özellikle Windows’tan publish alırken Linux runtime belirtmek iyi olur.

### Eksik runtime

Belirti:

- `Failed to load the .NET runtime`
- `It was not possible to find any compatible framework version`

Kontrol:

```bash
dotnet --info
```

### Yanlış environment variable

Belirti:

- DB connection string bulunamaz
- JWT ayarları boş gelir
- RabbitMQ host yanlış olur

Özellikle API için [AddInfrastructure](../src/SourceEx.Infrastructure/DependencyInjection.cs) bağlantı dizesi boşsa uygulamayı başlatmaz.

### Database erişim problemi

Belirti:

- `/health/ready` başarısız olur
- API açılır ama veritabanı işlemleri patlar

Kontrol:

```bash
docker compose ps
docker logs sourceex-postgres
```

veya PostgreSQL erişimi:

```bash
psql -h 127.0.0.1 -U postgres -d sourceex
```

### RabbitMQ erişim problemi

Belirti:

- worker’lar bağlanamaz
- outbox publish çalışmaz

Kontrol:

```bash
docker logs sourceex-rabbitmq
```

UI:

```text
http://<vm-ip>:15672
```

### Ollama erişim problemi

Belirti:

- policy worker log’da warning üretir
- fallback rule ile devam eder

Bu projede güzel taraf şu: Ollama erişilemezse policy worker tamamen çökmez, deterministic fallback kullanır.

## 12. Bu proje için en mantıklı deployment akışı

Bu repo’nun bugünkü haliyle benim önerdiğim en mantıklı yerel production simülasyonu akışı budur:

### Adım 1: Geliştirme makinesinde doğrula

Önce:

```bash
dotnet build SourceEx.slnx
```

Ardından gerekli migration’ı oluştur ve DB’yi hazırla. Unutma: repo içinde migration yok.

### Adım 2: Publish al

API ve üç worker için ayrı ayrı `dotnet publish` çalıştır.

### Adım 3: Linux VM’de altyapıyı hazırla

Linux içine:

- .NET 10 runtime veya SDK
- Nginx
- Docker + Compose

kur.

### Adım 4: Infra container’larını başlat

Repo’daki [docker-compose.yml](../docker-compose.yml) ile:

```bash
docker compose up -d
```

Bu sana:

- PostgreSQL
- RabbitMQ
- Ollama

sağlar.

### Adım 5: Publish dosyalarını Linux’a kopyala

Örneğin:

```bash
scp -r ./publish/api user@linux-vm:/opt/sourceex/api/current
scp -r ./publish/worker-notification user@linux-vm:/opt/sourceex/workers/notification/current
scp -r ./publish/worker-audit user@linux-vm:/opt/sourceex/workers/audit/current
scp -r ./publish/worker-policy user@linux-vm:/opt/sourceex/workers/policy/current
```

### Adım 6: Environment file’ları oluştur

Örnek API env dosyası:

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

Policy worker env örneği:

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

### Adım 7: systemd servislerini tanımla

API ve worker servislerini `/etc/systemd/system` altında oluştur.

### Adım 8: Servisleri ayağa kaldır

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now sourceex-api
sudo systemctl enable --now sourceex-worker-notification
sudo systemctl enable --now sourceex-worker-audit
sudo systemctl enable --now sourceex-worker-policy
```

### Adım 9: Nginx’i bağla

Nginx config ile `127.0.0.1:5005` üzerindeki API’ye proxy ver.

### Adım 10: Test et

Sırayla:

```bash
curl http://127.0.0.1:5005/health/live
curl http://127.0.0.1:5005/health/ready
curl http://<vm-ip>/health/live
curl http://<vm-ip>/api/v1.0/auth/token
```

Sonra expense oluşturma ve approve akışını çalıştır.

### Adım 11: Log kontrol et

```bash
journalctl -u sourceex-api -f
journalctl -u sourceex-worker-policy -f
journalctl -u sourceex-worker-audit -f
journalctl -u sourceex-worker-notification -f
```

Gerektiğinde:

```bash
sudo nginx -t
sudo systemctl status nginx
sudo systemctl status sourceex-api
docker compose ps
```

## Bu proje şu an production deployment için tamamen hazır mı?

Hayır, tam anlamıyla değil. Ama yerel production simülasyonu için yeterince iyi bir temel sunuyor.

Eksik veya geliştirilmesi gereken noktalar:

- `appsettings.Production.json` dosyaları yok
- EF migration’lar commit edilmemiş
- reverse proxy için forwarded headers desteği yok
- kalıcı dosya/sink tabanlı logging yok
- worker health endpoint veya watchdog mekanizması yok
- secrets yönetimi şu an basit düzeyde

Buna rağmen local Linux + Nginx + systemd + Docker altyapısı ile oldukça gerçekçi bir dağıtım provası yapılabilir.

## Son öneri

Bu proje için en mantıklı ilk production benzeri kurulum modeli şudur:

- **Nginx host üzerinde**
- **API ve worker’lar host üzerinde systemd ile**
- **PostgreSQL, RabbitMQ ve Ollama Docker Compose ile**
- **Config değerleri environment file ile**

Bu model hem anlaşılırdır hem de seni gerçek production yaklaşımına yaklaştırır. Daha sonra istersen ikinci aşamada:

- Dockerfile
- CI/CD publish pipeline
- HTTPS
- Serilog/Seq
- OpenTelemetry
- forwarded headers düzeltmesi

gibi production hazırlıklarını ekleyebilirsin.
