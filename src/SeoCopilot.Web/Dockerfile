# syntax=docker/dockerfile:1
#
# Web imaji. Build baglami bu klasor (compose'da context: ./src/SeoCopilot.Web).
# Vite statik dosya uretir, Caddy servis eder ve /api'yi api servisine vekiller.

# --- build ---
FROM node:22-alpine AS build
WORKDIR /app

# Kilit dosyasindan birebir kurulum; kaynak degisince bu katman bozulmasin.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# --- runtime ---
FROM caddy:2-alpine AS runtime

COPY --from=build /app/dist /srv
COPY Caddyfile /etc/caddy/Caddyfile

EXPOSE 80
