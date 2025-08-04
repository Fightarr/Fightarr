# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0.7 AS build
WORKDIR /src
COPY Fightarr.sln .
COPY src/Fightarr.Core/Fightarr.Core.csproj src/Fightarr.Core/
COPY src/Fightarr.Data/Fightarr.Data.csproj src/Fightarr.Data/
COPY src/Fightarr.Web/Fightarr.Web.csproj src/Fightarr.Web/
RUN dotnet restore src/Fightarr.Web/Fightarr.Web.csproj
COPY src/ src/
WORKDIR /src/src/Fightarr.Web
RUN dotnet publish -c Release -o /app/publish

# Use LinuxServer.io base for *Arr compatibility
FROM lscr.io/linuxserver/baseimage-ubuntu:jammy
RUN apt-get update && apt-get install -y \
    dotnet-runtime-9.0.7 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish /app
COPY docker/root/ /

EXPOSE 17878
