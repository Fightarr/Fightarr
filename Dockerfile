# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY Fightarr.sln .

# Copy project files
COPY src/Fightarr.Core/Fightarr.Core.csproj src/Fightarr.Core/
COPY src/Fightarr.Data/Fightarr.Data.csproj src/Fightarr.Data/
COPY src/Fightarr.Web/Fightarr.Web.csproj src/Fightarr.Web/

# Restore dependencies
RUN dotnet restore src/Fightarr.Web/Fightarr.Web.csproj

# Copy source code
COPY src/Fightarr.Core/ src/Fightarr.Core/
COPY src/Fightarr.Data/ src/Fightarr.Data/
COPY src/Fightarr.Web/ src/Fightarr.Web/

# Build the application
WORKDIR /src/src/Fightarr.Web
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r fightarr && useradd -r -g fightarr fightarr

# Create directories
RUN mkdir -p /config /logs /downloads /media && \
    chown -R fightarr:fightarr /app /config /logs /downloads /media

# Copy published application
COPY --from=build /app/publish .

# Set ownership
RUN chown -R fightarr:fightarr /app

# Switch to non-root user
USER fightarr

# Expose port
EXPOSE 7878

# Environment variables
ENV ASPNETCORE_URLS=http://+:7878
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/config/fightarr.db"
ENV Logging__LogLevel__Default=Information
ENV Logging__LogLevel__Microsoft.AspNetCore=Warning

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:7878/api/system/health || exit 1

# Set entrypoint
ENTRYPOINT ["dotnet", "Fightarr.Web.dll"]
