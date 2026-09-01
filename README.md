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
| `PATCH /api/sites/{id}` | Kismi guncelleme: `name`, `baseUrl`, `isActive`, `scheduleCron`, `defaultBrandProfileId`, `crawlSettings`. **`baseUrl` degisirse dogrulama sifirlanir ve token yenilenir** |
| `DELETE /api/sites/{id}` | Siteyi ve tarama gecmisini siler (`204`) |
| `POST /api/sites/{id}/verify` | Kok sayfada `<meta name="seocopilot-verification" content="...">` arar; bulursa `verified_at` yazar |
| `PATCH /api/sites/{id}/crawl-settings` | Kismi guncelleme — verilmeyen alanlar korunur |
| `POST /api/sites/{id}/crawls` | Tarama baslatir → `{crawlId}`. Site dogrulanmamissa `400` |
| `GET /api/sites/{id}/crawls?page=&size=` | Sitenin tarama gecmisi, sayfalanmis |
| `POST /api/crawls` | `{siteId}` ile ayni islem (eski ucu, korunuyor) |
| `GET /api/crawls/{id}` | Ozet: durum, skorlar, `issue_counts`, ilk 100 bulgu — FE bunu yoklar |
| `POST /api/crawls/{id}/cancel` | Iptal isaretini yazar; calisan worker sonraki derinlik gecisinde durur. Bitmis tarama icin `400` |
| `GET /api/crawls/{id}/pages` | Filtreli + sayfalanmis: `url`, `statusCode`, `minStatusCode`, `maxStatusCode`, `depth`, `hasIssues`, `page`, `size` |
| `GET /api/crawls/{id}/issues` | Filtreli + sayfalanmis: `severity`, `minSeverity`, `category`, `ruleCode`, `status`, `pageId`, `page`, `size` |
| `GET /api/crawls/{id}/compare/{prevId}` | Iki tarama farki: skor/sayfa/onem deltasi, yeni ve cozulen bulgular |
| `GET /api/pages/{id}` | Sayfa detayi: kolonlar + `ogData`, `mainText` (ilk 20 000 karakter) ve sayfanin bulgulari |
| `POST /api/issues/{id}/ignore` | `{reason, applyToSite}` → bulguyu kapatir, `issue_ignores` yazar. `applyToSite` ayni kuralin tum acik bulgularini da kapatir |
| `POST /api/issues/{id}/reopen` | Bulguyu geri acar, susturma kaydini siler |

Sayfalama zarfi her yerde ayni: `{ items, total, page, size }`; `size` en fazla 200.
Enum filtreleri hem `structured_data` hem `StructuredData` bicimini kabul eder; tanimsiz deger `400` doner.

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

Kurallar iki kumede calisir: **sayfa basina** ([`RuleEngine`](src/SeoCopilot.Rules/RuleEngine.cs), tek sayfanin
kendi verisine bakar) ve **crawl basina** ([`CrawlRules`](src/SeoCopilot.Rules/CrawlRules.cs), tum sayfa/link
kumesine ve site gerceklerine bakar).

| Kategori | Sayfa basina | Crawl basina |
| --- | --- | --- |
| Indexability | `ROBOTS_NOINDEX` · `BROKEN_PAGE_4XX` · `SERVER_ERROR_5XX` · `REDIRECT_CHAIN` (>1 atlama) · `CANONICAL_MISSING` · `CANONICAL_POINTS_ELSEWHERE` | `BLOCKED_BY_ROBOTS_TXT` · `SITEMAP_MISSING` · `PAGE_NOT_IN_SITEMAP` |
| Meta | `META_TITLE_MISSING` · `META_TITLE_TOO_SHORT` (<30) · `META_TITLE_TOO_LONG` (>60) · `META_DESC_MISSING` · `META_DESC_TOO_LONG` (>160) | `META_TITLE_DUPLICATE` · `META_DESC_DUPLICATE` |
| Content | `H1_MISSING` · `H1_MULTIPLE` · `THIN_CONTENT` (<300 kelime) · `HEADING_HIERARCHY_BROKEN` | `DUPLICATE_CONTENT` |
| Links | `GENERIC_ANCHOR_TEXT` | `BROKEN_INTERNAL_LINK` · `ORPHAN_PAGE` · `TOO_DEEP` (>4) |
| Images | `IMAGE_MISSING_ALT` · `IMAGE_TOO_LARGE` (>200 KB) | — |
| Structured data & i18n | `SCHEMA_MISSING` · `OG_TAGS_MISSING` · `LANG_ATTR_MISSING` | — |
| Performance (PSI) | — | `LCP_POOR` (>4 sn) · `CLS_POOR` (>0.25) · `INP_POOR` (>500 ms) |

Notlar:

- Kural kodlari `rules` tablosu seed'i ile birebir ayni olmak zorundadir — `issues.rule_code` ona FK.
- Sayfa 2xx donmuyorsa yalniz ulasilabilirlik kodlari (`BROKEN_PAGE_4XX`, `SERVER_ERROR_5XX`, `REDIRECT_CHAIN`) raporlanir; getirilemeyen sayfa (`status_code = 0`) `SERVER_ERROR_5XX` sayilir.
- `PAGE_NOT_IN_SITEMAP`, `ORPHAN_PAGE`, `META_*_DUPLICATE` yalniz **dizinlenebilir** sayfalar icin uretilir (2xx + noindex yok). Sitemap hic bulunamazsa `SITEMAP_MISSING` yazilir ve `PAGE_NOT_IN_SITEMAP` bastirilir.
- `IMAGE_TOO_LARGE` icin crawler sayfa basina `Crawler:MaxImageChecksPerPage` kadar gorselin boyutunu HEAD ile olcer (sonuclar crawl boyunca onbelleklenir). Deger `0` ise olcum kapanir ve kural sessiz kalir.
- Performans kurallari yalniz `PageSpeed:ApiKey` tanimliysa calisir: kok sayfa icin PSI cagrilir, olcum `vitals` tablosuna yazilir ve kurallara beslenir. PSI hata verirse crawl etkilenmez.

Skor: severity basina sabit ceza (critical 25, high 15, medium 8, low 3) 100'den dusulur.
`crawls.overall_score` = sayfa skorlarinin ortalamasi eksi crawl seviyesi cezalar — crawl seviyesi ceza
**kural kodu basina bir kez** uygulanir, boylece 300 oksuz sayfa skoru tek basina sifirlamaz.
`category_scores` kategori basina ayni formul (ceza sayfa sayisina bolunur);
`scoring_snapshot` kullanilan ceza tablosunu dondurur.

## Marka & icerik uretimi

| Endpoint | Aciklama |
| --- | --- |
| `GET /api/brand-profiles?siteId=` | Kiracinin marka profilleri; `siteId` verilirse o siteye ozel + kiraci geneli profiller |
| `POST /api/brand-profiles` | `{name, siteId?, tone?, addressForm?, emojiUsage?, bannedPhrases[], defaultHashtags[], targetAudience?, extraContext?, isDefault?}` |
| `PATCH /api/brand-profiles/{id}` | Kismi guncelleme |
| `GET /api/platform-profiles` | Seed listesi (instagram, facebook, x, linkedin) — karakter/hashtag sinirlari ve prompt notu |
| `POST /api/content/generate` | `{type, platformCode?, pageId?, brandProfileId?, input, variantCount?}` → is kuyruga girer, `201` + is kaydi |
| `POST /api/content/generate-batch` | `{type, platformCode, pageIds[], brandProfileId?, input?}` → sayfa basina bir is, `202` + `{jobIds}` |
| `GET /api/content/jobs/{id}` | Is durumu + varyantlar (FE polling) |
| `GET /api/content/jobs` | Gecmis uretimler; `type`, `platformCode`, `status`, `siteId`, `pageId`, `page`, `size` |
| `POST /api/content/variants/{id}/favorite` | Govdesiz cagri favoriye ekler; `{isFavorite:false}` cikarir |
| `GET /api/content/export.csv?jobIds=a,b,c` | Varyantlari CSV olarak indirir (UTF-8 BOM, Excel uyumlu) |

`type` = `title` · `meta_description` · `h1` · `product_description` · `blog_outline` · `fix_advice` ·
`social_post` · `social_batch` · `hashtag_set`. Son uc tur icin `platformCode` zorunludur.

Uretim **senkron degildir**: istek `content_jobs` satirini `queued` olarak yazar ve Hangfire'a atar; worker
marka profilini, platform kurallarini ve (verilmisse) sayfa baglamini prompt'a enjekte edip Anthropic
Messages API'yi cagirir. Model yaniti `{"variants":[{angle,body,hashtags,cta}]}` semasinda beklenir; sema
disi yanit bosa gitmesin diye ham metin tek varyant olarak yazilir. `Anthropic:ApiKey` tanimli degilse is
`failed` olur ve hata mesaji `content_jobs.error_message`'a yazilir.

## Rapor, performans ve pano

| Endpoint | Aciklama |
| --- | --- |
| `GET /api/sites/{id}/vitals?url=&take=` | PSI olcum gecmisi: `{latest, history}` |
| `POST /api/sites/{id}/reports` | `{crawlId?, compareCrawlId?, periodStart?, periodEnd?}` — `crawlId` verilmezse son tarama. Rapor kuyruga girer |
| `GET /api/sites/{id}/reports` | Sitenin raporlari |
| `GET /api/reports/{id}` | Rapor durumu + hazirsa `downloadUrl` |
| `GET /api/reports/{id}/download` | Rapor dosyasi (HTML). Hazir degilse `400` |
| `GET /api/dashboard` | Kiraci ozeti: site kartlari (son skor + onceki taramaya gore degisim), acik bulgu dagilimi, son taramalar, son icerik uretimleri |

Rapor HTML olarak uretilir (harici varlik icermez, tarayicidan PDF'e basilabilir) ve
`Reports:Directory` altina `reports/{id}.html` olarak yazilir.

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

`InitialCreate` ve `RuleCatalogueRework` uretildi ([Persistence/Migrations](src/SeoCopilot.Infrastructure/Persistence/Migrations)).
`RuleCatalogueRework` kural katalogunu yeniler; once yeni kurallari ekler, mevcut `issues` satirlarini yeni
kodlara tasir (`HTTP_STATUS` → sayfanin durum koduna gore `BROKEN_PAGE_4XX`/`SERVER_ERROR_5XX` gibi), ardindan
eski kural satirlarini siler. Karsiligi kalmayan `SLOW_TTFB`, `HREFLANG_INVALID` ve esik alti
`META_DESCRIPTION_LENGTH` bulgulari **silinir**.

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
| `Crawler:MaxImageChecksPerPage` | `IMAGE_TOO_LARGE` icin sayfa basina HEAD ile olculecek gorsel sayisi (varsayilan 10, `0` → kapali) |
| `Anthropic:ApiKey` | Anthropic Messages API |
| `PageSpeed:ApiKey` | Google PSI. Bos ise performans kurallari (`LCP_POOR`/`CLS_POOR`/`INP_POOR`) hic calismaz |
| `Reports:Directory` | Uretilen rapor dosyalarinin kok dizini (varsayilan `App_Data/reports`) |
| `Smtp:*` | Rapor maili |
