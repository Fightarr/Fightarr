# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Fightarr.sln .
COPY src/Fightarr.Core/Fightarr.Core.csproj src/Fightarr.Core/
COPY src/Fightarr.Data/Fightarr.Data.csproj src/Fightarr.Data/
COPY src/Fightarr.Web/Fightarr.Web.csproj src/Fightarr.Web/

RUN dotnet restore src/Fightarr.Web/Fightarr.Web.csproj

COPY src/ src/
WORKDIR /src/src/Fightarr.Web
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - Microsoft base (simpler)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install su-exec for user management
RUN apt-get update && apt-get install -y \
    su-exec \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy application
COPY --from=build /app/publish .

# Copy init script
COPY docker/init.sh /init.sh
RUN chmod +x /init.sh

# Create directories
RUN mkdir -p /config /logs

EXPOSE 17878
ENTRYPOINT ["/init.sh"]
