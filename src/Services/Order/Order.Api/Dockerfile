# syntax=docker/dockerfile:1
# Order service: orders, status history and the saga orchestrator.
# Build from the repository root:  docker build -f src/Services/Order/Order.Api/Dockerfile .

# ---- build: restore and publish with the .NET 10 SDK -------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from project files only, so the NuGet layer is cached until a project file changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Store.Contracts.Events/Store.Contracts.Events.csproj src/Shared/Store.Contracts.Events/
COPY src/Shared/Store.Messaging/Store.Messaging.csproj src/Shared/Store.Messaging/
COPY src/Shared/Store.Observability/Store.Observability.csproj src/Shared/Store.Observability/
COPY src/Services/Order/Order.Api/Order.Api.csproj src/Services/Order/Order.Api/
RUN dotnet restore src/Services/Order/Order.Api/Order.Api.csproj

# The typed Catalog client is generated from the OpenAPI contract during the build.
COPY contracts/ contracts/
COPY src/Shared/Store.Contracts.Events/ src/Shared/Store.Contracts.Events/
COPY src/Shared/Store.Messaging/ src/Shared/Store.Messaging/
COPY src/Shared/Store.Observability/ src/Shared/Store.Observability/
COPY src/Services/Order/Order.Api/ src/Services/Order/Order.Api/
RUN dotnet publish src/Services/Order/Order.Api/Order.Api.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---- runtime: ASP.NET Core runtime only, non-root, curl for the health check -------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

# The aspnet image ships a non-root "app" user ($APP_UID).
USER $APP_UID

HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Order.Api.dll"]
