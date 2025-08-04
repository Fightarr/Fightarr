#!/bin/bash

# Default values
PUID=${PUID:-1000}
PGID=${PGID:-1000}

echo "Starting Fightarr with PUID=$PUID, PGID=$PGID"

# Create group if it doesn't exist
if ! getent group $PGID >/dev/null 2>&1; then
    groupadd -g $PGID fightarr
fi

# Create user if it doesn't exist
if ! getent passwd $PUID >/dev/null 2>&1; then
    useradd -u $PUID -g $PGID -d /config -s /bin/bash fightarr
fi

# Create and set permissions on directories
mkdir -p /config /logs
chown -R $PUID:$PGID /config /logs

# Set environment variables
export ASPNETCORE_URLS="http://+:17878"  # Changed from 7878
export ASPNETCORE_ENVIRONMENT="Production"
export ConnectionStrings__DefaultConnection="Data Source=/config/fightarr.db"

# Start application as the specified user
exec su-exec $PUID:$PGID dotnet Fightarr.Web.dll
