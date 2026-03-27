## Logging Setup - Serilog & Seq

### Overview

The MarketData service is configured with **Serilog** for structured logging with multiple sinks:
- **Console**: Real-time logging in the terminal
- **File**: Persistent logs in the `logs/` directory
- **Seq**: Centralized structured logging for querying and analysis

### Installed Packages

The following NuGet packages have been added to the MarketData project:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />    
<PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
<PackageReference Include="Serilog.Enrichers.Process" Version="3.0.0" />
<PackageReference Include="Serilog.Enrichers.Span" Version="3.1.0" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
<PackageReference Include="Serilog.Extensions.Logging" Version="10.0.0" />
<PackageReference Include="Serilog.Settings.Configuration" Version="10.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
<PackageReference Include="Serilog.Sinks.Seq" Version="9.0.0" />
```

### Configuration

#### appsettings.json

Serilog is configured in `appsettings.json` with the following settings:

- **Minimum Log Level**: Information (Warning for ASP.NET Core and Entity Framework)
- **Console Sink**: Outputs formatted logs to the console
- **File Sink**: 
  - Location: `logs/marketdata-{Date}.log`
  - Rolling: Daily
  - Size Limit: 10 MB per file
  - Retention: 7 days
- **Seq Sink**: Sends structured logs to Seq at `http://localhost:5341`

#### appsettings.Development.json

Development environment overrides:
- **Minimum Log Level**: Debug for MarketData namespace
- **ASP.NET Core**: Information level
- **Entity Framework**: Information level

### Setting Up Seq

#### Option 1: Docker (Recommended)

Run Seq in a Docker container:

##### Option 1: Set Admin Password (Recommended for Production)

**Docker Run:**
```bash
docker run -d \
  --name seq \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINPASSWORD=YourSecurePassword123! \
  -p 5341:80 \
  -v seq-data:/data \
  datalust/seq:latest
```

**Docker Compose:**
```yaml
version: '3.8'
services:
  seq:
    image: datalust/seq:latest
    container_name: seq
    environment:
      - ACCEPT_EULA=Y
      - SEQ_FIRSTRUN_ADMINPASSWORD=YourSecurePassword123!
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data
    restart: unless-stopped

volumes:
  seq-data:
```

##### Option 2: Disable Authentication (Development Only)

**Docker Run:**
```bash
docker run -d \
  --name seq \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_NOAUTHENTICATION=true \
  -p 5341:80 \
  -v seq-data:/data \
  datalust/seq:latest
```

**Docker Compose:**
```yaml
version: '3.8'
services:
  seq:
    image: datalust/seq:latest
    container_name: seq
    environment:
      - ACCEPT_EULA=Y
      - SEQ_FIRSTRUN_NOAUTHENTICATION=true
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data
    restart: unless-stopped

volumes:
  seq-data:
```

#### Option 2: Windows Installation

1. Download Seq from: https://datalust.co/download
2. Run the installer
3. Seq will be available at `http://localhost:5341`

#### Accessing Seq

Once running, open your browser to:
- **UI**: http://localhost:5341
- **API**: http://localhost:5341/api

Default credentials (if authentication enabled):
- Username: `admin`
- Password: (set during installation)

### Usage Examples

#### Viewing Logs in Seq

1. Start the MarketData service
2. Open http://localhost:5341 in your browser
3. Use the search bar to filter logs:
   - `@Level = 'Error'` - Show only errors
   - `SourceContext like '%PriceSimulator%'` - Show simulator logs
   - `@Message like '%instrument%'` - Search for specific text
   - `RequestPath = '/api/instruments'` - Filter by HTTP endpoint


### Request Logging

HTTP requests are automatically logged with the following information:
- Request method and path
- Response status code
- Response time
- Request host
- User agent

Example log entry:
```
HTTP GET /api/instruments responded 200 in 45.2 ms
```

### File Logging

Logs are written to the `logs/` directory:
- **File pattern**: `marketdata-YYYY-MM-DD.log`
- **Retention**: 7 days
- **Max size**: 10 MB per file (then rolls to new file)

### Troubleshooting

#### Seq Not Receiving Logs

1. **Check Seq is running**:
   ```bash
   docker ps | grep seq
   ```

2. **Verify connection**:
   ```bash
   curl http://localhost:5341/api
   ```

3. **Check configuration**: Ensure `appsettings.json` has correct Seq URL

#### Performance Considerations

- **Trace logging**: Use sparingly in production (high volume)
- **Async logging**: Serilog buffers logs asynchronously by default
- **Seq ingestion limits**: Free version has 30-day retention

### Additional Resources

- [Serilog Documentation](https://serilog.net/)
- [Seq Documentation](https://docs.datalust.co/docs)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Best-Practices)
