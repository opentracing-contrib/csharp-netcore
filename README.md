**WARNING: This project is a work in progress and not yet ready for production**

# OpenTracing instrumentation for .NET Core apps

This repository provides OpenTracing instrumentation for .NET Core based applications.
It can be used with any OpenTracing compatible tracer.

## Supported .NET versions

This project currently only supports apps targeting `netcoreapp2.0` (.NET Core 2.0) or higher!

## Supported libraries and frameworks

This project supports any library or framework that uses .NET's [`DiagnosticSource`](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md)
to instrument its code. It will create a span for every [`Activity`](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md)
and it will create `span.Log` calls for all other diagnostic events.

To further improve the tracing output, the library provides enhanced instrumentation
(Inject/Extract, tags, configuration options) for the following libraries / frameworks:

* ASP.NET Core
* Entity Framework Core
* .NET Core BCL types (HttpClient)

## Usage

This project depends on several packages from Microsofts new `Microsoft.Extensions.*` stack (e.g. Dependency Injection, Logging)
so its main use case is ASP.NET Core apps but it's also possible to instrument non-web based .NET Core apps like console apps, background services etc.
if they also use this stack.

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

Note that .NET Core 2.1 will greatly simplify this setup by introducing a generic `HostBuilder` that works similar to the existing `WebHostBuilder` from ASP.NET Core. Have a look at the `TrafficGenerator` sample for an example of a `HostBuilder` based console application.
