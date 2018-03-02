**WARNING: This project is a work in progress and not yet ready for production**

# OpenTracing instrumentation for ASP.NET Core

This repository provides OpenTracing instrumentation for the ASP.NET Core framework. It can be used with any OpenTracing compatible implementation.

## Supported versions

The project currently only supports apps targeting `netcoreapp2.0` (.NET Core 2.0)!

## Usage in ASP.NET Core web apps

##### 1. Add the NuGet package `OpenTracing.Contrib.AspNetCore` to your web project.

##### 2. Add `.UseOpenTracing()` to the initialization code of your `IWebHostBuilder`. Typically, this is done in the `BuildWebHost` method of your `Program.cs`:

```csharp
public static IWebHost BuildWebHost(string[] args)
{
    return WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>()

        // Adds and starts the OpenTracing instrumentation
        .UseOpenTracing()

        .Build();
}
```
Note that calling this method starts the instrumentation, even if you've not added an OpenTracing compatible tracer. It will however use the [`NoopTracer`](https://github.com/opentracing/opentracing-csharp/tree/master/src/OpenTracing/Noop) in that case, so overhead should be minimal.

## Usage in .NET Core based console apps

It's also possible to instrument non-web based .NET Core apps like console apps, background services etc. by using the separate `OpenTracing.Contrib.NetCore` package. It depends on several packages from Microsofts new `Microsoft.Extensions.*` stack (e.g. Dependency Injection, Logging) so be aware that your application has to use them as well.

Have a look at the `TrafficGenerator` sample for an example.

##### 1. Add the NuGet package `OpenTracing.Contrib.NetCore` to your web project.

##### 2. Add the OpenTracing services to your `IServiceCollection` via `services.AddOpenTracing()`.

How you do this depends on how you've setup the `Microsoft.Extensions.DependencyInjection` system in your app.

```csharp
services.AddOpenTracing();
```

##### 3. Aquire an instance of `IOpenTracingInstrumentor` and `Start()` the instrumentation.

After you've set up your DI container, you have to manually start the instrumentation. Note that this is not necessary in ASP.NET core apps.

```csharp
serviceProvider.GetRequiredService<IOpenTracingInstrumentor>().Start();
```

Note that .NET Core 2.1 will greatly simplify this setup by introducing a `GenericHostBuilder` that works similar to the existing `WebHostBuilder` from ASP.NET Core.

## Instrumented components

The following components will be instrumented.

#### Outgoing HTTP calls (.NET Core and ASP.NET Core)

Any code that uses `HttpClientHandler` (e.g. `HttpClient`) will be instrumented and any OpenTracing state (e.g. baggage) will automatically be added to the headers of the outgoing request.

You can configure the behavior (e.g. the "component" tag) by calling `builder.ConfigureHttpOut()` in your `.UseOpenTracing()` method:

```csharp
webHostBuilder.UseOpenTracing(builder =>
{
    builder.ConfigureHttpOut(options =>
    {
        options.ComponentName = "CustomName";
    });
})
```

You can find all possible configuration options in [`HttpOutOptions.cs`](https://github.com/opentracing-contrib/csharp-aspnetcore/blob/master/src/OpenTracing.Contrib.NetCore/Interceptors/HttpOut/HttpOutOptions.cs)

#### Entity Framework Core commands (.NET Core and ASP.NET Core)

There will be a span for every executed command.

#### Incoming ASP.NET Core requests (ASP.NET Core)

There will be a span for every incoming request and any incoming OpenTracing state (e.g. baggage) will automatically be extracted.

#### ASP.NET Core MVC components (ASP.NET Core)

There will be a span for every executed action and view result.
