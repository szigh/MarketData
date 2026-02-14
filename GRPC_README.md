# Market Data gRPC Implementation

This implementation uses gRPC server streaming to push real-time price updates to clients.

## Architecture

### Server Side (MarketData API)
- **Proto file**: `Protos/marketdata.proto` - Defines the gRPC service contract
- **gRPC Service**: `Services/MarketDataGrpcService.cs` - Implements server streaming
- **Background Service**: Updated to broadcast prices via gRPC channel

### Client Side (MarketData.Client)
- Console application that subscribes to price updates
- Uses the same proto file to generate client code
- Receives real-time price streams

## How It Works

1. **Price Generation**: Background service generates prices at configured intervals
2. **Broadcasting**: Each new price is written to an unbounded channel
3. **Streaming**: gRPC service reads from the channel and streams to subscribed clients
4. **Filtering**: Only prices for subscribed instruments are sent to each client

## Running the Application

### 1. Update the Database
```bash
dotnet ef database update --project MarketData
```

### 2. Create Test Instruments
Use the API to create instruments (via Scalar UI at `https://localhost:7150/scalar/v1`):

```json
POST /api/instruments
{
  "name": "FTSE",
  "initialPrice": 8000,
  "tickIntervalSeconds": 1
}

POST /api/instruments
{
  "name": "AAPL",
  "initialPrice": 180,
  "tickIntervalSeconds": 5
}
```

### 3. Start the API
```bash
dotnet run --project MarketData
```

The API will start on `https://localhost:7150`

### 4. Run the Client
In a new terminal:
```bash
dotnet run --project MarketData.Client
```

When prompted, enter instruments to subscribe (e.g., `FTSE,AAPL`)

## Output Example

```
Market Data gRPC Client
======================

Enter instruments to subscribe (comma-separated, e.g., FTSE,AAPL): FTSE,AAPL

Subscribing to: FTSE, AAPL
Waiting for price updates... (Press Ctrl+C to exit)

[14:23:45.123] FTSE       8024.3421
[14:23:46.234] FTSE       8026.1234
[14:23:50.456] AAPL       180.5678
[14:23:47.345] FTSE       8023.9876
[14:23:55.567] AAPL       180.4321
```

## Key Features

✅ **Real-time streaming** - No polling required
✅ **Multiple instruments** - Subscribe to multiple instruments simultaneously
✅ **Efficient** - Binary protocol, low overhead
✅ **Selective subscription** - Only receive updates for subscribed instruments
✅ **Graceful shutdown** - Ctrl+C handling

## gRPC vs REST

**Why gRPC for this scenario:**
- Real-time push (no polling)
- Binary protocol (more efficient)
- Built-in streaming support
- Strongly-typed contracts

**REST Alternative:**
- Would require polling or SignalR
- More overhead for high-frequency updates
- JSON serialization overhead

## Port Configuration

Default port is `https://localhost:7150`. If your API runs on a different port:

1. Check `MarketData/Properties/launchSettings.json`
2. Update the client's channel address in `MarketData.Client/Program.cs`

## Interview Talking Points

1. **gRPC Benefits**: Binary protocol, code generation, streaming support
2. **Server Streaming**: One request, continuous response stream
3. **Channel Pattern**: Unbounded channel for producer-consumer pattern
4. **Proto Contracts**: Language-agnostic service definitions
5. **HTTP/2**: Multiplexing, header compression
