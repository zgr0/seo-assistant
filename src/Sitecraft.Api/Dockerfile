# syntax=docker/dockerfile:1
#
# API imaji. Build baglami repo koku (compose'da context: .) — Api butun katmanlara
# proje referansi veriyor, tek proje klasoru yetmez.
#
# Chromium dahil ve iki is birden goruyor: renderJs=true olan siteler Playwright ile
# getiriliyor, rapor PDF'i de ayni tarayicidan basiliyor. Tarayici olmadan o taramalar
# ve butun PDF raporlar calisma aninda patlardi.

# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Once yalniz proje dosyalari: kaynak degistiginde restore katmani bozulmasin.
COPY Directory.Build.props ./
COPY src/Sitecraft.Domain/Sitecraft.Domain.csproj                 src/Sitecraft.Domain/
COPY src/Sitecraft.Application/Sitecraft.Application.csproj       src/Sitecraft.Application/
COPY src/Sitecraft.Rules/Sitecraft.Rules.csproj                   src/Sitecraft.Rules/
COPY src/Sitecraft.Crawler/Sitecraft.Crawler.csproj               src/Sitecraft.Crawler/
COPY src/Sitecraft.Infrastructure/Sitecraft.Infrastructure.csproj src/Sitecraft.Infrastructure/
COPY src/Sitecraft.Api/Sitecraft.Api.csproj                       src/Sitecraft.Api/
RUN dotnet restore src/Sitecraft.Api/Sitecraft.Api.csproj

COPY src/ src/
RUN dotnet publish src/Sitecraft.Api/Sitecraft.Api.csproj \
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
         --runtimeconfig Sitecraft.Api.runtimeconfig.json \
         --depsfile Sitecraft.Api.deps.json \
         Microsoft.Playwright.dll install --with-deps chromium-headless-shell \
    && chmod -R a+rX /ms-playwright /app/.playwright \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Raporlar (Reports:Directory) ve sosyal gonderi gorselleri (Assets:Directory) buraya
# yazilir — compose'da ikisine de volume bagli. Klasor imajda yoksa Docker bos volume'u
# root sahipli acar ve `app` yazamaz ("Permission denied"); bu yuzden ikisi de burada
# `app` sahipli olusturulur, yeni volume sahipligi buradan kopyalar.
RUN mkdir -p /app/App_Data/reports /app/App_Data/assets && chown -R app:app /app/App_Data

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "Sitecraft.Api.dll"]
