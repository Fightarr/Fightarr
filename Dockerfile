# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Fightarr.sln .
COPY src/Fightarr.Core/Fightarr.Core.csproj src/Fightarr.Core/
COPY src/Fightarr.Data/Fightarr.Data.csproj src/Fightarr.Data/
COPY src/Fightarr.Web/Fightarr.Web.csproj src/Fightarr.Web/

# Restore dependencies
RUN dotnet restore src/Fightarr.Web/Fightarr.Web.csproj

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/Fightarr.Web
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - Use LinuxServer.io base with proper .NET installation
FROM lscr.io/linuxserver/baseimage-ubuntu:jammy

# Install .NET 9 runtime using Microsoft's official method
RUN apt-get update && apt-get install -y \
    wget \
    ca-certificates \
    && wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y aspnetcore-runtime-9.0 \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish /app

# Copy service configuration
COPY docker/root/ /

# Expose port
EXPOSE 17878

# Volume definitions
VOLUME ["/config"]
