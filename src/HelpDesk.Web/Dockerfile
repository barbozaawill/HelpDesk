# Build a partir da RAIZ do repositorio:
#   docker build -f src/HelpDesk.Web/Dockerfile -t helpdesk-web .

# ---------- etapa de build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/HelpDesk.Domain/HelpDesk.Domain.csproj src/HelpDesk.Domain/
COPY src/HelpDesk.Web/HelpDesk.Web.csproj src/HelpDesk.Web/
RUN dotnet restore src/HelpDesk.Web/HelpDesk.Web.csproj

COPY src/ src/
RUN dotnet publish src/HelpDesk.Web/HelpDesk.Web.csproj -c Release -o /app

# ---------- etapa de runtime (arquivos estaticos servidos pelo nginx) ----------
FROM nginx:alpine AS final
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY src/HelpDesk.Web/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080
