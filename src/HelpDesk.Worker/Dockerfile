# Build a partir da RAIZ do repositorio:
#   docker build -f src/HelpDesk.Worker/Dockerfile -t helpdesk-worker .

# ---------- etapa de build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/HelpDesk.Domain/HelpDesk.Domain.csproj src/HelpDesk.Domain/
COPY src/HelpDesk.Infrastructure/HelpDesk.Infrastructure.csproj src/HelpDesk.Infrastructure/
COPY src/HelpDesk.Worker/HelpDesk.Worker.csproj src/HelpDesk.Worker/
RUN dotnet restore src/HelpDesk.Worker/HelpDesk.Worker.csproj

COPY src/ src/
RUN dotnet publish src/HelpDesk.Worker/HelpDesk.Worker.csproj -c Release -o /app -p:UseAppHost=false

# ---------- etapa de runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HelpDesk.Worker.dll"]
