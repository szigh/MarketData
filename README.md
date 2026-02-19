# Solution Motivations

The MarketData solution is a distributed system for simulating, generating, and visualizing real-time market price data. It demonstrates modern .NET development practices including gRPC streaming, background services, WPF data visualization, and hot-reloadable configuration.

My motivations were to show off my skills in API design and WPF in a fintech system. **It is purely a hobby project.**

I first wanted to create a small application that could stream market data, and another application that could "book" (fake) trades based on that data. After soon realizing that market data would come at some expense I decided to start simulating that data. I implemented some basic models. I then wanted to see what effect different model parameters would have on the ticking prices, so one thing led to another and this is where I have got to.

This is still work in progress, but see below where I have got to so far! Key features like the trading screen, security, containerisation and a more scalable RDBMS (currently I use SQLite) are yet to be implemented.

# MarketData Solution Architecture

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Applications                      │
├─────────────────────┬───────────────────┬───────────────────────┤
│  MarketData.Wpf     │  MarketData       │    FastSimulate       │
│     .Client         │    .Client        │                       │
│  (WPF GUI Client)   │ (Console Client)  │  (Quick Test App)     │
└──────────┬──────────┴──────────┬────────┴───────────────────────┘
           │                     │
           │   gRPC Streaming    │
           └──────────┬──────────┘
                      │
           ┌──────────▼──────────┐
           │    MarketData       │
           │   (ASP.NET Core)    │
           │    gRPC Server      │
           └──────────┬──────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
   ┌────▼────┐   ┌────▼────┐  ┌─────▼───────┐
   │ SQLite  │   │  Model  │  │   Price     │
   │   DB    │   │ Manager │  │ Simulator   │
   └─────────┘   └─────────┘  └─────────────┘
```

## Projects

### 1. **MarketData** (ASP.NET Core Server)
**Type:** ASP.NET Core Web API with gRPC  
**Framework:** .NET 10  
**Key Technologies:** Entity Framework Core, gRPC, SQLite

**Purpose:**  
The core backend service that generates and streams real-time market data to clients.

**Key Components:**
- **Background Service:** `MarketDataGeneratorService` continuously generates price updates
- **gRPC Services:**
  - `MarketDataService` - Streams real-time price updates to subscribers
  - `ModelConfigurationGrpcService` - Manages price simulation model configurations
- **REST API:** Controllers for instruments and prices (via OpenAPI/Scalar)
- **Database:** SQLite for persisting instruments, prices, and model configurations
- **Model Management:** `IInstrumentModelManager` for hot-reloadable configuration

**Key Features:**
- Real-time price generation using multiple simulation models
- Hot reload of model configurations without service restart
- Per-instrument configurable tick intervals
- Streaming via gRPC for efficient real-time updates
- Persistent storage of price history

**Dependencies:**
- `MarketData.PriceSimulator` - Price simulation algorithms

---

### 2. **MarketData.Wpf.Client** (WPF Application)
**Type:** WPF Desktop Application  
**Framework:** .NET 10 Windows  
**Key Technologies:** WPF, gRPC Client, FancyCandles (chart control), MVVM pattern

**Purpose:**  
Rich desktop client for visualizing market data with real-time candlestick charts and model configuration.

**Key Components:**
- **ViewModels:** MVVM pattern with `MainWindowViewModel`, `InstrumentViewModel`, `InstrumentTabViewModel`
- **Views:** Multi-tab interface with candlestick charts and model configuration panels
- **gRPC Client:** Subscribes to real-time price streams from the server
- **Chart Integration:** FancyCandles library for professional candlestick visualization
- **Model Configuration UI:** Dynamic forms for editing simulation parameters

**Key Features:**
- Real-time candlestick charts with configurable timeframes
- Multi-instrument tabs
- Live model configuration editing with hot reload
- Visual representation of price simulation behavior
- Async initialization 

**Dependencies:**
- `MarketData.Wpf.Shared` - Shared WPF utilities (RelayCommand, ViewModelBase)
- `MarketData.Client.Shared` - Shared gRPC configuration
- Proto files linked from `MarketData` project

---

### 3. **MarketData.Client** (Console Application)
**Type:** Console Application  
**Framework:** .NET 10

**Purpose:**  
Lightweight console client for subscribing to market data streams. Useful for testing and demonstrations.

**Key Features:**
- Simple command-line interface
- Subscribe to multiple instruments
- Real-time console output of price updates
- Graceful shutdown on Ctrl+C

**Dependencies:**
- `MarketData.Client.Shared` - Shared gRPC configuration
- Proto files from `MarketData` project

---

### 4. **MarketData.PriceSimulator** (Class Library)
**Type:** Class Library  
**Framework:** .NET 10

**Purpose:**  
Core price simulation engine with multiple mathematical models for generating realistic market data.

**Price Simulation Models:**
- **RandomMultiplicativeProcess** - Percentage-based random walk
- **MeanRevertingProcess** - Ornstein-Uhlenbeck mean reversion
- **RandomAdditiveWalk** - Configurable step-based walk with custom patterns
- **Flat** - Static pricing (for testing)

**Key Interface:**
```csharp
public interface IPriceSimulator
{
    Task<double> GenerateNextPrice(double price);
}
```

**Features:**
- Each model implements `IPriceSimulator` for polymorphic usage
- Configurable parameters per model
- Stateless design (receives current price, returns next price)
- Used by both server and `FastSimulate` project

---

### 5. **FastSimulate** (Console Application)
**Type:** Quick Testing Console App  
**Framework:** .NET 10

**Purpose:**  
Standalone console application for rapidly testing and visualizing price simulator behavior without the full server infrastructure.

**Use Case:**  
Quick iteration and tuning of simulator parameters during development.

---

### 6. **MarketData.Wpf.Shared** (Class Library)
**Type:** Class Library  
**Framework:** .NET 10 Windows

**Purpose:**  
Shared WPF infrastructure components.

**Contents:**
- `ViewModelBase` - Base class for MVVM view models with INotifyPropertyChanged
- `RelayCommand` - ICommand implementation for MVVM
- Common WPF utilities

---

### 7. **MarketData.Client.Shared** (Class Library)
**Type:** Class Library  
**Framework:** .NET 10

**Purpose:**  
Shared configuration classes for gRPC clients.

**Contents:**
- `GrpcSettings` - Configuration model for server URL binding

---

## Data Flow

### 1. Price Generation Flow
```
[Timer Tick] 
    → [MarketDataGeneratorService]
        → [IPriceSimulator.GenerateNextPrice()]
            → [New Price]
                → [SQLite DB]
                → [gRPC Broadcast]
                    → [Connected Clients]
```

### 2. Model Configuration Flow
```
[Client UI Change]
    → [gRPC ModelConfiguration.UpdateConfig()]
        → [InstrumentModelManager]
            → [Validate & Update DB]
                → [Raise ConfigurationChanged Event]
                    → [MarketDataGeneratorService.HotReload()]
                        → [New Simulator Instance]
```

### 3. Client Subscription Flow
```
[Client Connect]
    → [SubscribeToPrices(instruments)]
        → [Server Stream Setup]
            → [MarketDataGeneratorService.RegisterSubscriber]
                → [Continuous Price Updates via gRPC Stream]
```

---

## Key Design Patterns

### 1. **Dependency Injection**
All projects use constructor injection for loose coupling and testability.

### 2. **Strategy Pattern**
`IPriceSimulator` allows swapping different price simulation algorithms at runtime.

### 3. **Factory Pattern**
- `PriceSimulatorFactory` creates appropriate simulators based on configuration
- `InstrumentViewModelFactory` creates view models with proper dependencies

### 4. **Observer Pattern**
- `IInstrumentModelManager.ConfigurationChanged` event for hot reload
- gRPC streaming for real-time price updates

### 5. **MVVM Pattern**
WPF client strictly follows MVVM for separation of concerns and testability.

### 6. **Repository Pattern**
Entity Framework DbContext abstracts data access.

---

## Communication Protocols

### gRPC Services

#### MarketDataService
**Proto:** `marketdata.proto`

```protobuf
service MarketDataService {
  rpc SubscribeToPrices (SubscribeRequest) returns (stream PriceUpdate);
  rpc GetHistoricalData (HistoricalDataRequest) returns (HistoricalDataResponse);
}
```

**Purpose:** Real-time price streaming and historical data retrieval

#### ModelConfigurationService
**Proto:** `modelconfiguration.proto`

**Purpose:** Remote configuration management for price simulation models

**Features:**
- Switch active model per instrument
- Update model-specific parameters
- Update tick intervals
- Hot reload without reconnection

---

## Database Schema

### Tables
- **Instruments** - Market instruments (stocks, indices, etc.)
- **Prices** - Time-series price data
- **RandomMultiplicativeConfig** - Configuration for multiplicative random walk
- **MeanRevertingConfig** - Configuration for mean-reverting process
- **FlatConfig** - Configuration for flat pricing
- **RandomAdditiveWalkConfig** - Configuration for additive walk with steps

### Relationships
- Each `Instrument` has one configuration per model type (one-to-one)
- `Instrument.ModelType` determines active model
- All configurations cascade delete with instrument

---

## Configuration

### Server Configuration
**File:** `MarketData/appsettings.json`
- Database connection string
- `MarketDataGeneratorOptions` - Generator settings
- Kestrel endpoints for HTTP/HTTPS/gRPC

### Client Configuration
**File:** `MarketData.Wpf.Client/appsettings.json` / `MarketData.Client/appsettings.json`
- `GrpcSettings.ServerUrl` - Server endpoint (default: `https://localhost:7264`)

---

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Backend Framework | ASP.NET Core 10.0 |
| Client Framework | WPF (.NET 10 Windows) |
| RPC Framework | gRPC |
| Database | SQLite with Entity Framework Core |
| ORM | Entity Framework Core 10.0 |
| Charting | FancyCandles 2.7.1 |
| API Documentation | OpenAPI with Scalar |
| Background Processing | IHostedService |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |

---

## Development Workflow

### Running the Solution

1. **Start the Server:**
   ```bash
   cd MarketData
   dotnet run
   ```
   Server runs on `https://localhost:7264` (default)

2. **Start WPF Client:**
   ```bash
   cd MarketData.Wpf.Client
   dotnet run
   ```

3. **Start Console Client:**
   ```bash
   cd MarketData.Client
   dotnet run
   ```

4. **Quick Simulator Testing:**
   ```bash
   cd FastSimulate
   dotnet run
   ```

### Database Migrations
```bash
cd MarketData
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

---

## Extension Points

### Adding New Price Simulators
1. Create new class implementing `IPriceSimulator` in `MarketData.PriceSimulator`
2. Add configuration model to `MarketData/Models/`
3. Update `MarketDataContext` with new DbSet and relationship
4. Update `PriceSimulatorFactory` to handle new model
5. Add gRPC configuration methods to `ModelConfigurationGrpcService`
6. Create WPF view for configuration in `MarketData.Wpf.Client/Views/ModelConfigs/`

### Adding New Instruments
1. Insert into `Instruments` table
2. Create initial price record
3. Set desired `ModelType` and create corresponding configuration
4. Restart server or use hot reload API

---

## Performance Considerations

- **gRPC Streaming:** Efficient binary protocol with HTTP/2 multiplexing
- **Background Service:** Asynchronous price generation prevents blocking
- **Concurrent Dictionaries:** Thread-safe price simulator management
- **Entity Framework:** AsNoTracking for read-only queries
- **WPF Async:** Proper async/await patterns prevent UI freezing
- **Hot Reload:** Individual instrument updates without full restart

---

## Security Considerations

- Server runs on HTTPS by default
- gRPC uses HTTP/2 with TLS
- No authentication currently implemented (suitable for local development)

---

## Testing Strategy

To be implemented in future iterations, but potential approaches include:

- **Unit Tests:** Test individual simulators in isolation
- **Integration Tests:** Test gRPC services with real database
- **Manual Testing:** Use console client and `FastSimulate` for quick verification
- **UI Testing:** WPF client for visual validation of behavior

---

## Future Enhancements

- Authentication and authorization
- Trade execution simulation
- Multi-user support with isolated workspaces
- More sophisticated simulation models (volatility clustering, jump processes)
- Technical indicators and analytics
- Performance metrics and monitoring
- Docker containerization
- Cloud deployment (Azure, AWS)

---

## Project Dependencies Graph

```
MarketData.Wpf.Client
    ├── MarketData.Wpf.Shared
    ├── MarketData.Client.Shared
    └── (Proto files from MarketData)

MarketData.Client
    ├── MarketData.Client.Shared
    └── (Proto files from MarketData)

MarketData
    └── MarketData.PriceSimulator

FastSimulate
    └── MarketData.PriceSimulator

MarketData.Wpf.Shared
    └── (No dependencies)

MarketData.Client.Shared
    └── (No dependencies)

MarketData.PriceSimulator
    └── (No dependencies)
```
