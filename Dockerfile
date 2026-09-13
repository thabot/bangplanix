# Multi-stage Production Dockerfile for Bangplanix (.NET 10 / Alpine Linux)
# Optimized for Ultra-Fast Cold Start and Low Memory Footprint

# Stage 1: Build & Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS builder
WORKDIR /src

# Copy CPM and Solution files
COPY Directory.Build.props Directory.Packages.props Bangplanix.slnx ./
COPY src/ src/
COPY proto/ proto/
COPY tools/ tools/

# Build and Publish Bangplanix.Server
RUN dotnet publish src/Bangplanix.Server/Bangplanix.Server.csproj -c Release -o /app/publish

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime

# Install native Skia & Font dependencies on Alpine
RUN apk add --no-cache \
    fontconfig \
    freetype \
    libstdc++ \
    libgcc \
    icu-libs \
    curl

WORKDIR /app

# Create non-root user (UID 10001) for Zero-Trust container security
RUN addgroup -g 10001 bangplanix && \
    adduser -u 10001 -G bangplanix -s /bin/sh -D bangplanix && \
    mkdir -p /app/volumes/data /app/volumes/templates /app/volumes/fonts /app/volumes/logs && \
    chown -R bangplanix:bangplanix /app

COPY --from=builder --chown=bangplanix:bangplanix /app/publish /app

# Set Environment Variables
ENV ASPNETCORE_URLS=http://+:9545 \
    BANGPLANIX_HTTP_PORT=9545 \
    BANGPLANIX_GRPC_PORT=9546 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

# Switch to Non-Root User
USER bangplanix

# Expose HTTP REST (9545) and gRPC (9546) Ports
EXPOSE 9545 9546

# Liveness Probe Healthcheck
HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:9545/health || exit 1

ENTRYPOINT ["dotnet", "Bangplanix.Server.dll"]
