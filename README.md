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
