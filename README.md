[![nuget](https://img.shields.io/nuget/v/OpenTracing.Contrib.NetCore.svg?logo=nuget)](https://www.nuget.org/packages/OpenTracing.Contrib.NetCore)

# OpenTracing instrumentation for .NET Core apps

This repository provides OpenTracing instrumentation for .NET Core based applications.
It can be used with any OpenTracing compatible tracer.

_**IMPORTANT:** OpenTracing and OpenCensus have merget to form **[OpenTelemetry](https://opentelemetry.io)**! The OpenTelemetry .NET library can be found at [https://github.com/open-telemetry/opentelemetry-dotnet](https://github.com/open-telemetry/opentelemetry-dotnet)._

## Supported .NET versions

This project currently only supports apps targeting `netcoreapp2.1` (.NET Core 2.1), `netcoreapp3.1` (.NET Core 3.1), net5.0 (.NET 5.0), or .NET 6.0!

This project DOES NOT support the full .NET framework as that uses different instrumentation code.

## Supported libraries and frameworks

#### DiagnosticSource based instrumentation

This project supports any library or framework that uses .NET's [`DiagnosticSource`](https://github.com/dotnet/runtime/blob/master/src/libraries/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md)
to instrument its code. It will create a span for every [`Activity`](https://github.com/dotnet/runtime/blob/master/src/libraries/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md)
and it will create `span.Log` calls for all other diagnostic events.

To further improve the tracing output, the library provides enhanced instrumentation
(Inject/Extract, tags, configuration options) for the following libraries / frameworks:

* ASP.NET Core
* Entity Framework Core
* System.Net.Http (HttpClient)
* System.Data.SqlClient
* Microsoft.Data.SqlClient

#### Microsoft.Extensions.Logging based instrumentation

This project also adds itself as a logger provider for logging events from the `Microsoft.Extensions.Logging` system.
It will create `span.Log` calls for each logging event, however it will only create them if there is an active span (`ITracer.ActiveSpan`).

## Usage

This project depends on several packages from Microsofts `Microsoft.Extensions.*` stack (e.g. Dependency Injection, Logging)
so its main use case is ASP.NET Core apps and any other Microsoft.Extensions-based console apps.

##### 1. Add the NuGet package `OpenTracing.Contrib.NetCore` to your project.

##### 2. Add the OpenTracing services to your `IServiceCollection` via `services.AddOpenTracing()`.

How you do this depends on how you've setup the `Microsoft.Extensions.DependencyInjection` system in your app.

In ASP.NET Core apps you can add the call to your `ConfigureServices` method (of your `Program.cs` file):

```csharp
public static IWebHost BuildWebHost(string[] args)
{
    return WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>()
        .ConfigureServices(services =>
        {
            // Enables and automatically starts the instrumentation!
            services.AddOpenTracing();
        })
        .Build();
}
```

##### 3. Make sure `InstrumentationService`, which implements `IHostedService`, is started.

`InstrumentationService` is responsible for starting and stopping the instrumentation.
The service implements `IHostedService` so **it is automatically started in ASP.NET Core**,
however if you have your own console host, you manually have to call `StartAsync` and `StopAsync`.

Note that .NET Core 2.1 greatly simplified this setup by introducing a generic `HostBuilder` that works similar to the existing `WebHostBuilder` from ASP.NET Core. Have a look at the `TrafficGenerator` sample for an example of a `HostBuilder` based console application.
