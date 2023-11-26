using CustomersApi.DataStore;
using Microsoft.EntityFrameworkCore;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost.UseUrls(Constants.CustomersUrl);

// Registers and starts Jaeger (see Shared.JaegerServiceCollectionExtensions)
builder.Services.AddJaeger();

// Enables OpenTracing instrumentation for ASP.NET Core, CoreFx, EF Core
builder.Services.AddOpenTracing(ot =>
{
    ot.ConfigureAspNetCore(options =>
    {
        // We don't need any tracing data for our health endpoint.
        options.Hosting.IgnorePatterns.Add(ctx => ctx.Request.Path == "/health");
    });

    ot.ConfigureEntityFrameworkCore(options =>
    {
        // This is an example for how certain EF Core commands can be ignored.
        // As en example, we're ignoring the "PRAGMA foreign_keys=ON;" commands that are executed by Sqlite.
        // Remove this code to see those statements.
        options.IgnorePatterns.Add(cmd => cmd.Command.CommandText.StartsWith("PRAGMA"));
    });
});

// Adds a Sqlite DB to show EFCore traces.
builder.Services.AddDbContext<CustomerDbContext>(options =>
{
    options.UseSqlite("Data Source=DataStore/customers.db");
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<CustomerDbContext>();


var app = builder.Build();


// Load some dummy data into the db.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    dbContext.Seed();
}


// Configure the HTTP request pipeline.

app.MapGet("/", () => "Customers API");

app.MapHealthChecks("/health");

app.MapGet("/customers", async (CustomerDbContext dbContext) => await dbContext.Customers.ToListAsync());

app.MapGet("/customers/{id}", async (int id, CustomerDbContext dbContext, ILogger<Program> logger) =>
{
    var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);

    if (customer == null)
        return Results.NotFound();

    // ILogger events are sent to OpenTracing as well!
    logger.LogInformation("Returning data for customer {CustomerId}", id);

    return Results.Ok(customer);
});

app.Run();
