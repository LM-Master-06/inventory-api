# ──────────────────────────────────────────────
# Stage 1: Restore & Build
# ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer-caching of restore
COPY src/InventoryAPI/InventoryAPI.csproj ./src/InventoryAPI/
RUN dotnet restore src/InventoryAPI/InventoryAPI.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish src/InventoryAPI/InventoryAPI.csproj \
        --configuration Release \
        --output /app/publish \
        --no-restore

# ──────────────────────────────────────────────
# Stage 2: Runtime image (minimal footprint)
# ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Data volume for SQLite database
RUN mkdir /data && chown appuser:appgroup /data
VOLUME ["/data"]

COPY --from=build /app/publish .

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "InventoryAPI.dll"]
