# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY GatekeeperHQ.sln .
COPY GatekeeperHQ.Domain/GatekeeperHQ.Domain.csproj GatekeeperHQ.Domain/
COPY GatekeeperHQ.Application/GatekeeperHQ.Application.csproj GatekeeperHQ.Application/
COPY GatekeeperHQ.Infrastructure/GatekeeperHQ.Infrastructure.csproj GatekeeperHQ.Infrastructure/
COPY GatekeeperHQ.API/GatekeeperHQ.API.csproj GatekeeperHQ.API/

# Restore dependencies
RUN dotnet restore GatekeeperHQ.API/GatekeeperHQ.API.csproj

# Copy remaining source code
COPY GatekeeperHQ.Domain/ GatekeeperHQ.Domain/
COPY GatekeeperHQ.Application/ GatekeeperHQ.Application/
COPY GatekeeperHQ.Infrastructure/ GatekeeperHQ.Infrastructure/
COPY GatekeeperHQ.API/ GatekeeperHQ.API/

# Build and publish
RUN dotnet publish GatekeeperHQ.API/GatekeeperHQ.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Railway injects PORT at runtime, default to 8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Use shell form to allow PORT env var substitution at runtime
CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet GatekeeperHQ.API.dll
