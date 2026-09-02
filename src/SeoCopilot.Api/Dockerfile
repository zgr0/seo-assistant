# syntax=docker/dockerfile:1
#
# API imaji. Build baglami repo koku (compose'da context: .) — Api butun katmanlara
# proje referansi veriyor, tek proje klasoru yetmez.
#
# Chromium dahil: renderJs=true olan siteler Playwright ile getiriliyor. Tarayici
# olmadan o taramalar calisma aninda patlardi.

# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Once yalniz proje dosyalari: kaynak degistiginde restore katmani bozulmasin.
COPY Directory.Build.props ./
COPY src/SeoCopilot.Domain/SeoCopilot.Domain.csproj                 src/SeoCopilot.Domain/
COPY src/SeoCopilot.Application/SeoCopilot.Application.csproj       src/SeoCopilot.Application/
COPY src/SeoCopilot.Rules/SeoCopilot.Rules.csproj                   src/SeoCopilot.Rules/
COPY src/SeoCopilot.Crawler/SeoCopilot.Crawler.csproj               src/SeoCopilot.Crawler/
COPY src/SeoCopilot.Infrastructure/SeoCopilot.Infrastructure.csproj src/SeoCopilot.Infrastructure/
COPY src/SeoCopilot.Api/SeoCopilot.Api.csproj                       src/SeoCopilot.Api/
RUN dotnet restore src/SeoCopilot.Api/SeoCopilot.Api.csproj

COPY src/ src/
RUN dotnet publish src/SeoCopilot.Api/SeoCopilot.Api.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish

# --- runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

COPY --from=build /app/publish ./

# curl: aspnet imajinda HTTP istemcisi yok, healthcheck'in ihtiyaci var.
#
# Tarayici olarak yalniz `chromium-headless-shell` kurulur — Playwright 1.49'dan beri
# `Chromium.LaunchAsync(Headless = true)` zaten bu ikiliyi calistiriyor. Tam Chrome ve
# ffmpeg (video kaydi) hic indirilmez; crawler ikisini de kullanmaz.
# UYARI: Headless = false yapilirsa bu imajda tarayici bulunamaz.
#
# Playwright CLI'si yayinlanan Microsoft.Playwright.dll uzerinden calistirilir — pwsh gerekmez.
#
# chmod a+rX iki yeri birden kapsar ve ikisi de sart, cunku uygulama `app` kullanicisiyla kosar:
#   /ms-playwright   — root olarak indirilen tarayici
#   /app/.playwright — Playwright surucusunun node ikilisi. Windows'ta publish edilince
#                      calistirma biti yalniz sahipte kalir (-rwxr--r--), `app` calistiramaz
#                      ve Playwright.CreateAsync() "Permission denied" ile duser.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && dotnet exec \
         --runtimeconfig SeoCopilot.Api.runtimeconfig.json \
         --depsfile SeoCopilot.Api.deps.json \
         Microsoft.Playwright.dll install --with-deps chromium-headless-shell \
    && chmod -R a+rX /ms-playwright /app/.playwright \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Raporlar (Reports:Directory) buraya yazilir — compose'da volume bagli.
RUN mkdir -p /app/App_Data/reports && chown -R app:app /app/App_Data

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "SeoCopilot.Api.dll"]
