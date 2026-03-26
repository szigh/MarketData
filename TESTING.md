## Testing Strategy

### **Test Projects Overview**

The solution has **comprehensive test coverage** with **220 automated tests** across critical components.

#### **MarketData.PriceSimulator.Tests**
**Frameworks:** xUnit v3 (3.2.2), Custom `[StatisticalFact]` attribute

**Coverage:**
- **Unit Tests**
  - Parameter validation
  - Edge case handling
  - Model correctness
- **Statistical Tests** - Stochastic validation (slower)
  - Chi-squared distribution tests
  - Mean/variance convergence
  - Long-term statistical properties

#### **MarketData.Tests**
**Frameworks:** xUnit v3 (3.2.2), Moq 4.20.72, EF Core InMemory 10.0.3, ASP.NET Testing 10.0.3

**Coverage:**
- **Controller Tests** - REST API endpoints
  - `InstrumentsController` - CRUD operations
  - `PricesController` - Price queries  
  - `ModelConfigurationsController` - Configuration management
- **gRPC Contract Tests** - Proto message structure stability
  - Compile-time breaking change detection
  - Message field validation
  - Backward compatibility protection
- **Service Unit Tests** - Business logic
  - `InstrumentModelManager` - Configuration CRUD, model switching, instrument management
  - Parameter validation
  - Event notifications
- **Integration Tests** - Background service lifecycle
  - Service startup/shutdown
  - Real-time tick interval verification

---

### **Testing Libraries**

| Library | Version | Purpose | Used In |
|---------|---------|---------|---------|
| **xUnit** | 3.2.2 | Test framework | All test projects |
| **Moq** | 4.20.72 | Mocking framework | MarketData.Tests |
| **EF Core InMemory** | 10.0.3 | Database testing | MarketData.Tests |
| **ASP.NET Testing** | 10.0.3 | Integration/API tests | MarketData.Tests |
| **coverlet.collector** | 6.0.4 | Code coverage | All test projects |

---

### **Test Execution**

```bash
# Fast tests only
dotnet test --environment RUN_STATISTICAL_TESTS=False

# All tests including slower statistical
dotnet test --environment RUN_STATISTICAL_TESTS=True
```

#### **By Project:**

```bash
dotnet test MarketData.PriceSimulator.Tests
dotnet test MarketData.Tests
```

---

### **Testing Approaches by Component**

#### ** Unit Tests** (Business Logic)
- Price models, service layer, configuration management
- Fast, isolated tests with mocked dependencies
- **Tools:** xUnit, Moq, InMemory database

#### ** Statistical Tests** (Mathematical Correctness)
- Distribution properties, mean/variance, convergence
- Generate 10,000+ samples, apply Chi-squared tests
- **Tools:** Custom `[StatisticalFact]` attribute, xUnit

#### ** Contract Tests** (API Stability)
- gRPC message structure, field presence
- Compile-time validation, structural assertions
- **Tools:** xUnit, Proto-generated classes

#### ** Integration Tests** (Lifecycle & Timing)
- Background service, real-time behavior
- Real service host with InMemory database
- **Tools:** xUnit, Microsoft.Extensions.Hosting

#### ** Manual Testing** (UI & End-to-End)
- WPF client, console client, user workflows
- Manual verification during development
- **Tools:** Visual Studio debugging, production WPF app

---

### **Untested Projects (By Design)**

#### **MarketData.Wpf.Client**
- Low ROI compared to manual testing
- Backend APIs already comprehensively tested
- Manual UI testing during development
- ViewModels could be unit tested (future enhancement)

#### **MarketData.Client**, **FastSimulate**
- Simple console application with no business logic

#### **MarketData.Wpf.Shared**
- Simple utility classes

#### **MarketData.Client.Shared**
- Contains only configuration POCOs (`GrpcSettings`)
