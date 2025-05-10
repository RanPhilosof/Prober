## Prober – Lightweight Runtime Data Probing with Minimal Application Impact
Prober is a lightweight .NET library that enables efficient runtime data monitoring from your application with minimal overhead and app impact. It offers flexible probing mechanisms that cache data internally and expose it through a REST API – ideal for debugging, UI visualization, logging, or real-time telemetry.

## 🌟 Key Features
Minimal Impact: Data is only processed when requested (lazy conversion).

### Flexible Probing Types:
* Cyclic Cached Prober – Circular buffer with fixed size.
* Key-Value Cached Prober – Object cache accessed by ID.
* Single Object Prober – Holds and exposes a single data object.
* Custom Conversion: Convert input objects to tabular List<List<string>> format for easy UI integration.
* Built-in REST API Support: Just start a Kestrel server in your app to expose data.
* Generic UI Support: Thanks to the uniform tabular output format, UIs can generically render any probed data.


## 🔧 How It Works

When you create a Prober instance, you define:
1. The data type you want to probe.
2. A converter function to transform the data to a table-like format (List<List<string>>).
3. Header metadata for visualization.
  The actual data conversion only happens on demand (e.g., when a UI requests it via the API).
All active probers are registered into a central "club" that manages and serves data to any registered consumer (UI, logger, etc.).

## 🚀 Example: Cyclic Cache Prober

C# Code:
```csharp
var prober = new CyclicCacheProbing<Measurment>(
  maxCachedValues: 100,
  name: "Measurement Prober",
  headers: Measurment.Headers,
  headerType: HeaderType.Standard);

prober.Convert = (measure) => new List<string> {
  $"{measure.Counter}",
  $"{measure.Height}",
  $"{measure.Width}",
  $"{measure.Size}",
  $"{measure.Description}"
};

// Simulate adding random data
Task.Run(() =>
  {
    while (true)
    {
      Thread.Sleep(100);
      
      prober.EnqueueCyclic(new Measurment(
        height: Random.Shared.Next(1, 10),
        width: Random.Shared.Next(11, 20),
        size: Random.Shared.Next(21, 30),
        description: GetRandomWord()));
    }
  });
```

## 📡 REST API for External Access
To expose probed data, integrate this controller in your ASP.NET Core app:

C# Code:
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProberCacheMonitoringController : ControllerBase
{
    [HttpGet("CachedTablesNames")]
    public ActionResult<List<ExtendedTableInfo>> GetCachedTablesNames()
    {
        return ProberCacheClub.ProberCacheClubSingleton.GetCachedTablesNames();
    }

    [HttpPost("CachedTables")]
    public ActionResult<List<Table>> GetCachedTables([FromBody] List<Guid> tablesGuid)
    {
        return ProberCacheClub.ProberCacheClubSingleton.GetCachedTables(tablesGuid);
    }
}
```
    
Just launch your Kestrel web server, and you're ready to remotely inspect live application data!

## 💡 Use Cases
* Real-time UI dashboards
* File/CSV/DB recording
* Debug visualization
* Remote telemetry in distributed systems

## 📦 Install via NuGet
dotnet add package RP.Prober

## 🖥️ Blazor UI for Viewing Prober Data
The RP.Prober.Razor.Component NuGet package provides a generic Blazor UI component that can automatically discover and display live data from multiple running applications that use the Prober system.

## 📦 Required NuGet Packages
To integrate the Prober UI into your Blazor WebAssembly or Server project, make sure to include the following NuGet packages:

Blazor App Csproj Dependencies:
```csharp
<PackageReference Include="RP.Infra" Version="1.0.3" />
<PackageReference Include="RP.Prober" Version="1.0.7" />
<PackageReference Include="RP.Prober.Razor.Component" Version="1.0.7" />
<PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
```

## 🔧 Register Required Services
In your Program.cs (or wherever you configure services), add the following lines:

C# Code At Blazor Page At Program.cs:
```csharp
builder.Services.AddSingleton<ProberCacheMonitoringService>(); // Consumes data from multiple apps
builder.Services.AddSingleton<IServicesInfo>(new ServicesInfo()); // Discovers available apps/services to probe
builder.Services.AddBlazoredLocalStorage(); // Needed by the Razor component for state management
```

## 🌐 Configure Services Discovery (Optional but Recommended)
If you're using the built-in ServicesInfo implementation (recommended for simpler setups), you need to define environment variables that describe each app that Prober should connect to.

Here’s an example for a service called RanBlazor:

Environment Variables:
```xml
"Services:RanBlazor:Ip": "127.0.0.1",
"Services:RanBlazor:RestApiPort": "7287",
"Services:RanBlazor:SupportProberMonitor": "true",
"Services:RanBlazor:RestApiSecured": "true"
```
You can configure multiple services this way, each under its own key (e.g., Services:App1, Services:App2, etc.).

## 💻 Razor Page Integration
To display all the probed data in your app’s UI, add the following to any .razor page:

Razor Page:
```html
@page "/ProberDashboard"
@using RP.Prober.Razor.Component

<PageTitle>Prober Dashboard</PageTitle>

<ProberView />
```
The <ProberView /> component will auto-fetch and display data from all configured and running Prober-enabled services.

## 🔍 Summary
* All probed data from any number of apps is collected through ProberCacheMonitoringService.
* The IServicesInfo implementation (like ServicesInfo) provides discovery metadata based on your environment.
* The Blazor UI uses ProberView to render real-time data tables.
* No extra setup is needed in the UI per prober — once probers are registered and REST APIs are live, the UI picks them up.


Prober UI Screen Shot:

![Screenshot](https://github.com/RanPhilosof/Prober/blob/main/readme_screenshot.png?raw=true)


