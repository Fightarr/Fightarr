#!/bin/bash

PUID=${PUID:-1000}
PGID=${PGID:-1000}

echo "Starting Fightarr with PUID=$PUID, PGID=$PGID"

# Create group and user
groupadd -g $PGID fightarr 2>/dev/null || true
useradd -u $PUID -g $PGID -d /config -s /bin/bash fightarr 2>/dev/null || true

# Set permissions
mkdir -p /config /logs
chown -R $PUID:$PGID /config /logs

# Set environment
export ASPNETCORE_URLS="http://+:17878"
export ConnectionStrings__DefaultConnection="Data Source=/config/fightarr.db"

# Start app
exec su-exec $PUID:$PGID dotnet Fightarr.Web.dll
