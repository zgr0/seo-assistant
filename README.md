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

Playwright tarayicisi (Crawler icin bir kez):

```bash
pwsh src/SeoCopilot.Crawler/bin/Debug/net10.0/playwright.ps1 install chromium
```

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
dotnet test tests/SeoCopilot.Rules.Tests
dotnet test tests/SeoCopilot.Api.Tests    # Docker gerekli
```

## Konfigurasyon (appsettings / user-secrets)

| Anahtar | Aciklama |
| --- | --- |
| `ConnectionStrings:Postgres` | EF Core + Hangfire storage |
| `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` | Bearer token dogrulama |
| `Anthropic:ApiKey` | Anthropic Messages API |
| `PageSpeed:ApiKey` | Google PSI (opsiyonel) |
| `Smtp:*` | Rapor maili |
