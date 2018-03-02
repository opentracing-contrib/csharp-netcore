**WARNING: This project is a work in progress and not yet ready for production**

# OpenTracing instrumentation for ASP.NET Core

This repository provides OpenTracing instrumentation for the ASP.NET Core framework. It can be used with any OpenTracing compatible implementation.

## Supported versions

The project currently only supports ASP.NET Core apps targeting `netcoreapp2.0` (ASP.NET Core 2.0 on .NET Core)!

## Usage

1. Add the NuGet package `OpenTracing.Contrib.AspNetCore` to your web project.

2. Add `.UseOpenTracing()` to the initialization code of your `IWebHostBuilder`. Typically, this is done in the `BuildWebHost` method of your `Program.cs`:

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

## Instrumented components

The following components will be instrumented.

#### Outgoing HTTP calls

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

You can find all possible configuration options in [`HttpOutOptions.cs`](https://github.com/opentracing-contrib/csharp-aspnetcore/blob/master/src/OpenTracing.Contrib.Core/Interceptors/HttpOut/HttpOutOptions.cs)

#### Entity Framework Core commands

There will be a span for every executed command.

#### Incoming ASP.NET Core requests

There will be a span for every incoming request and any incoming OpenTracing state (e.g. baggage) will automatically be extracted.

#### ASP.NET Core MVC components (actions, views)

There will be a span for every executed action and view result.
