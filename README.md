# SeoCopilot

Site tarayan, kural motoruyla SEO skoru cikaran ve rapor maili gonderen servis.

## Yapi

```
SeoCopilot.slnx
├── src/
│   ├── SeoCopilot.Domain/          Entity, enum, domain kurallari — bagimlilik yok
│   ├── SeoCopilot.Application/     Servisler, DTO, use-case, arayuzler (Abstractions/)
│   ├── SeoCopilot.Infrastructure/  EF Core DbContext + repo, AnthropicClient, PsiClient, SmtpEmailSender
│   ├── SeoCopilot.Crawler/         Playwright + AngleSharp, robots/sitemap, PageExtractor
│   ├── SeoCopilot.Rules/           Kural motoru + handler'lar + skor hesabi (saf, test edilebilir)
│   ├── SeoCopilot.Api/             Minimal API, JWT auth, Hangfire dashboard (/hangfire)
│   └── SeoCopilot.Web/             React + Vite (ayri build, Caddy servis eder)
└── tests/
    ├── SeoCopilot.Rules.Tests/     Kural + skor birim testleri
    ├── SeoCopilot.Crawler.Tests/   URL normalizasyon, robots, sitemap, HTML cikarma (ag'siz)
    ├── SeoCopilot.Auth.Tests/      JWT + bcrypt birim testleri
    └── SeoCopilot.Api.Tests/       Testcontainers (Postgres) ile entegrasyon
```

### Bagimlilik yonu

```
Domain  <-  Application  <-  Infrastructure
                 ^      <-  Crawler
                 |
Rules (yalniz Domain'e bagli)  --  Api'de RuleRunnerAdapter ile baglanir
Api  ->  Application + Infrastructure + Crawler + Rules
```

## Calistirma

Postgres gerekli (docker):

```bash
docker run -d --name seocopilot-pg -e POSTGRES_DB=seocopilot -e POSTGRES_USER=seocopilot -e POSTGRES_PASSWORD=seocopilot -p 5432:5432 postgres:16-alpine
```

API:

```bash
dotnet run --project src/SeoCopilot.Api
```

Web (dev):

```bash
cd src/SeoCopilot.Web && npm run dev
```

Playwright tarayicisi — yalnizca `renderJs: true` kullanilacaksa, bir kez:

```bash
pwsh src/SeoCopilot.Api/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Auth

JWT bearer + rotasyonlu refresh token. Sifreler **bcrypt** (work factor 12).

| Endpoint | Aciklama |
| --- | --- |
| `POST /api/auth/register` | `{email,password,fullName,tenantName}` → yeni trial tenant + owner user, token cifti |
| `POST /api/auth/login` | `{email,password}` → token cifti |
| `POST /api/auth/refresh` | `{refreshToken}` → eski token iptal, yeni cift (rotasyon) |
| `POST /api/auth/logout` | `{refreshToken}` → refresh token iptal |

Access token: `sub`, `email`, `name`, `tenant_id`, `role` claim'leri; varsayilan 15 dk.
Refresh token: istemciye ham deger, DB'de yalniz `sha256` hash (`refresh_tokens.token_hash`), 30 gun.
E-posta kayitta global benzersiz kabul edilir (DB kisiti `unique(tenant_id,email)`).

Korumali endpoint ornegi: `Authorization: Bearer <accessToken>` → `POST /api/crawls`.

Ayarlar (`appsettings.json` → `Jwt`): `Key` (>=32 bayt, prod'da user-secrets), `Issuer`, `Audience`, `AccessTokenMinutes`, `RefreshTokenDays`.

Tum `/api/sites` ve `/api/crawls` uclari token'daki `tenant_id` ile sinirlanir — baska kiracinin kaydi `404` doner.

## Site & tarama

### Endpoint'ler

| Endpoint | Aciklama |
| --- | --- |
| `POST /api/sites` | `{name,baseUrl,crawlSettings?}` → site + `verificationToken`. `baseUrl` normalize edilir (sema+host, sonda `/` yok) |
| `GET /api/sites` | Kiracinin siteleri |
| `GET /api/sites/{id}` | Tek site |
| `POST /api/sites/{id}/verify` | Kok sayfada `<meta name="seocopilot-verification" content="...">` arar; bulursa `verified_at` yazar |
| `PATCH /api/sites/{id}/crawl-settings` | Kismi guncelleme — verilmeyen alanlar korunur |
| `POST /api/crawls` | `{siteId}` → crawl kuyruga girer (Hangfire). Site dogrulanmamissa `400` |
| `GET /api/crawls/{id}` | Ozet: durum, skorlar, `issue_counts`, ilk 100 bulgu |
| `GET /api/crawls/{id}/pages?page=&size=` | Sayfalanmis sayfa listesi (size en fazla 200) |
| `GET /api/crawls/{id}/issues?minSeverity=` | Bulgular; `minSeverity` = `low\|medium\|high\|critical` |

### Tarama motoru

[`CrawlEngine`](src/SeoCopilot.Application/Services/CrawlEngine.cs) seviye seviye BFS yapar:

- **Tohum**: `base_url` + `robots.txt`'deki (yoksa `/sitemap.xml`) sitemap URL'leri. Kok URL robots/desen filtrelerinden muaftir.
- **robots.txt**: RFC 9309 — ardisik `User-agent` satirlari tek grup, en uzun desen kazanir, esitlikte `Allow` oncelikli, `*`/`$` desteklenir. Kendi token'imiz (`seocopilotbot`) `*`'a gore onceliklidir. Dosya yoksa kisit yok.
- **Es zamanlilik ve nezaket**: her seviye `Concurrency` paralellikte, her getirmeden sonra `DelayMs` beklenir (~`Concurrency/DelayMs` istek/sn). Getirme paralel, veritabanina yazma tek is parcaciginda.
- **Yonlendirme**: elle izlenir (`AllowAutoRedirect=false`), en fazla `Crawler:MaxRedirects` atlama. `status_code` zincirin sonundan, `redirect_to` varilan adresten gelir.
- **Ayristirma**: yalniz 2xx + `text/html`. Govde `Crawler:MaxHtmlBytes` ile sinirli. `nofollow` linkler `page_links`'e yazilir ama kuyruga alinmaz.
- **Durum**: normal bitis `completed`; `MaxPages`/`MaxDepth` yuzunden kuyrukta URL kaldiysa `partial`; iptal `cancelled`; kok URL alinamazsa `failed`. Tekil sayfa hatasi crawl'i dusurmez — `status_code = 0` yazilir ve `HTTP_STATUS` tetiklenir.
- **Link grafigi**: tarama sonrasi `page_links.to_page_id` cozulur, `pages.inlink_count` / `outlink_internal` / `outlink_external` hesaplanir.

`crawl_settings` (jsonb, site basina): `maxPages` (500), `maxDepth` (5), `delayMs` (500), `concurrency` (3), `renderJs` (false), `includePatterns`, `excludePatterns`. Desenler mutlak URL'e karsi **regex** (IgnoreCase, 1 sn timeout); `include` bos degilse en az biri eslesmeli.

`renderJs: true` ise sayfa Playwright/Chromium ile render edilir (JS calisir); aksi halde duz `HttpClient`.

### Kurallar ve skor

Sayfa basina kurallar: `HTTP_STATUS`, `NOINDEX_DETECTED`, `META_TITLE_MISSING`, `META_TITLE_LENGTH`, `META_DESCRIPTION_MISSING`, `META_DESCRIPTION_LENGTH`, `H1_MISSING`, `H1_MULTIPLE`, `CANONICAL_MISSING`, `THIN_CONTENT`, `IMAGE_ALT_MISSING`, `STRUCTURED_DATA_MISSING`.
Crawl basina kurallar ([`CrawlRules`](src/SeoCopilot.Rules/CrawlRules.cs)): `DUPLICATE_CONTENT` (ayni `content_hash`), `BROKEN_INTERNAL_LINK` (4xx/5xx donen ic link).

Kural kodlari `rules` tablosu seed'i ile birebir ayni olmak zorundadir — `issues.rule_code` ona FK. Sayfa 2xx donmuyorsa yalniz `HTTP_STATUS` raporlanir.

Skor: severity basina sabit ceza (critical 25, high 15, medium 8, low 3) 100'den dusulur.
`crawls.overall_score` = sayfa skorlarinin ortalamasi eksi crawl seviyesi cezalar;
`category_scores` kategori basina ayni formul (ceza sayfa sayisina bolunur);
`scoring_snapshot` kullanilan ceza tablosunu dondurur.

## Veritabani

PostgreSQL, EF Core code-first. Snake_case kolon/tablo adlari (`EFCore.NamingConventions`).
Enum'lar `snake_case` metin olarak saklanir (`SnakeCaseEnumConverter`).

### Tablolar

| Grup | Tablolar |
| --- | --- |
| Kiraci & kullanici | `tenants`, `users` (citext email), `refresh_tokens` |
| Site | `sites` (`crawl_settings` jsonb) |
| Tarama | `crawls` (`category_scores`/`issue_counts`/`scoring_snapshot` jsonb), `pages` (`url_hash` bytea, `h1_texts` text[]), `page_links` |
| Kural motoru | `rules` (SEED), `issues` (`evidence` jsonb), `issue_ignores` |
| Performans | `vitals` |
| Marka & icerik | `brand_profiles`, `platform_profiles` (SEED), `content_jobs`, `content_variants` |
| Rapor & sistem | `reports`, `audit_logs` (`ip` inet, `payload` jsonb) |

Entity siniflari: [src/SeoCopilot.Domain/Entities](src/SeoCopilot.Domain/Entities) (alt klasorlere ayrilmis).
EF yapilandirmasi: [src/SeoCopilot.Infrastructure/Persistence](src/SeoCopilot.Infrastructure/Persistence).

### Migration

`InitialCreate` uretildi ([Persistence/Migrations](src/SeoCopilot.Infrastructure/Persistence/Migrations)).
Uygulamak icin Postgres calisir olmali:

```bash
dotnet ef database update -p src/SeoCopilot.Infrastructure -s src/SeoCopilot.Api
```

Baglanti dizesi: `SEOCOPILOT_DB` ortam degiskeni (tasarim zamani) veya `ConnectionStrings:Postgres` (calisma zamani).
`citext` uzantisi ilk migration'da olusturulur — DB rolunun `CREATE EXTENSION` yetkisi olmali.

Yeni migration:

```bash
dotnet ef migrations add <Ad> -p src/SeoCopilot.Infrastructure -s src/SeoCopilot.Api -o Persistence/Migrations
```

## Test

```bash
dotnet test tests/SeoCopilot.Rules.Tests tests/SeoCopilot.Crawler.Tests tests/SeoCopilot.Auth.Tests
dotnet test tests/SeoCopilot.Api.Tests    # Docker gerekli
```

`SeoCopilot.Api.Tests` icindeki `CrawlEngineTests`, rastgele portta kucuk bir test sitesi
([`TestWebSite`](tests/SeoCopilot.Api.Tests/TestWebSite.cs)) ayaga kaldirip taramayi gercek HTTP
ve gercek Postgres uzerinde ucdan uca calistirir.

## Konfigurasyon (appsettings / user-secrets)

| Anahtar | Aciklama |
| --- | --- |
| `ConnectionStrings:Postgres` | EF Core + Hangfire storage |
| `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` | Bearer token dogrulama |
| `Crawler:UserAgent` / `Crawler:UserAgentToken` | Giden istek basligi ve robots.txt'de eslesecek token |
| `Crawler:RequestTimeoutSeconds` / `MaxRedirects` / `MaxHtmlBytes` / `MaxMainTextChars` | Getirme sinirlari |
| `Anthropic:ApiKey` | Anthropic Messages API |
| `PageSpeed:ApiKey` | Google PSI (opsiyonel) |
| `Smtp:*` | Rapor maili |
