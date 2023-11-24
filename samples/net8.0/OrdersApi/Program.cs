using Microsoft.EntityFrameworkCore;
using OrdersApi.DataStore;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost.UseUrls(Constants.OrdersUrl);

// Registers and starts Jaeger (see Shared.JaegerServiceCollectionExtensions)
builder.Services.AddJaeger();

// Enables OpenTracing instrumentation for ASP.NET Core, CoreFx, EF Core
builder.Services.AddOpenTracing(builder =>
{
    builder.ConfigureAspNetCore(options =>
    {
        // We don't need any tracing data for our health endpoint.
        options.Hosting.IgnorePatterns.Add(ctx => ctx.Request.Path == "/health");
    });
});

// Adds a SqlServer DB to show EFCore traces.
builder.Services.AddDbContext<OrdersDbContext>(options =>
{
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Orders-net5;Trusted_Connection=True;MultipleActiveResultSets=true");
});

builder.Services.AddSingleton<HttpClient>();

builder.Services.AddMvc();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>();


var app = builder.Build();


// Load some dummy data into the db.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    dbContext.Seed();
}


// Configure the HTTP request pipeline.

app.MapGet("/", () => "Orders API");

app.MapHealthChecks("/health");

app.MapDefaultControllerRoute();

app.Run();
