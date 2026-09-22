# Build a partir da RAIZ do repositorio:
#   docker build -f src/HelpDesk.Api/Dockerfile -t helpdesk-api .

# ---------- etapa de build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Primeiro so os .csproj -> o "dotnet restore" fica em cache enquanto eles nao mudam.
COPY src/HelpDesk.Domain/HelpDesk.Domain.csproj src/HelpDesk.Domain/
COPY src/HelpDesk.Infrastructure/HelpDesk.Infrastructure.csproj src/HelpDesk.Infrastructure/
COPY src/HelpDesk.Api/HelpDesk.Api.csproj src/HelpDesk.Api/
RUN dotnet restore src/HelpDesk.Api/HelpDesk.Api.csproj

# Agora o resto do codigo e publica.
COPY src/ src/
RUN dotnet publish src/HelpDesk.Api/HelpDesk.Api.csproj -c Release -o /app -p:UseAppHost=false

# ---------- etapa de runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HelpDesk.Api.dll"]
