# Fightarr

An automated MMA/UFC/Boxing event acquisition and management system for the *Arr ecosystem.

## Features

- Radarr-style web interface for managing fight events
- Quality profiles and release monitoring
- Integration with download clients (qBittorrent, Transmission, etc.)
- Support for UFC, Bellator, PFL, ONE Championship, and more
- Custom indexer support for MMA/Boxing releases
- Automatic metadata fetching for fight events

## Quick Start

### Docker Compose (Recommended)

1. Create a `docker-compose.yml` file:
```yaml
version: '3.8'

services:
  fightarr:
    image: fightarr/fightarr:latest
    container_name: fightarr
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=UTC
    volumes:
      - ./config:/config
      - ./logs:/logs
      - /path/to/downloads:/downloads
      - /path/to/media:/media
    ports:
      - "7878:7878"
    restart: unless-stopped



Run the application:
bash
docker-compose up -d
Access the web interface at http://localhost:7878
Manual Installation
Install .NET 8.0 Runtime
Download the latest release from GitHub
Extract and run:
bash
dotnet Fightarr.Web.dll
Configuration
Fightarr follows the same configuration patterns as other *Arr applications:

Web interface: http://localhost:7878
Config directory:
Linux: ~/.config/Fightarr/
Windows: %APPDATA%/Fightarr/
Docker: /config
Initial Setup
Download Clients: Configure qBittorrent, Transmission, or other supported clients
Indexers: Add your preferred torrent indexers or connect to Prowlarr
Quality Profiles: Set up quality preferences for different types of events
Root Folders: Configure where downloaded events should be stored
Download Clients
Fightarr supports the same download clients as other *Arr applications:

qBittorrent
Transmission
Deluge
SABnzbd
NZBGet
Indexers
Manual indexer configuration
Prowlarr integration (recommended)
Support for private trackers with MMA content
Quality Profiles
Configure quality preferences for:

PPV Events (highest quality)
Fight Nights (standard quality)
Prelims (lower quality acceptable)
File Naming
Fightarr uses intelligent file naming patterns:

{Event Name} ({Year}) {Quality}
UFC 294 Makhachev vs Volkanovski 2 (2023) 1080p WEB-DL
API
Fightarr provides a REST API compatible with the *Arr ecosystem for integration with other tools.

Support
Documentation: Wiki
Issues: GitHub Issues
Discord: Join our community
Contributing
Fork the repository
Create a feature branch
Make your changes
Add tests for new functionality
Submit a pull request
License
This project is licensed under the GPL v3 License - see the LICENSE file for details.

Acknowledgments
Built with inspiration from the excellent Radarr, Sonarr, and Lidarr projects
Part of the *Arr ecosystem
Special thanks to the MMA community for feedback and testing