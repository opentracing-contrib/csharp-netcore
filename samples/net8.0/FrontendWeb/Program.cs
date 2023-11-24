using Shared;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost.UseUrls(Constants.FrontendUrl);

// Registers and starts Jaeger (see Shared.JaegerServiceCollectionExtensions)
builder.Services.AddJaeger();

// Enables OpenTracing instrumentation for ASP.NET Core, CoreFx, EF Core
builder.Services.AddOpenTracing();

builder.Services.AddSingleton<HttpClient>();

builder.Services.AddMvc();


var app = builder.Build();


// Configure the HTTP request pipeline.

app.MapDefaultControllerRoute();

app.Run();
