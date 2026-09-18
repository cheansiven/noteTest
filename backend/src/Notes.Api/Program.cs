using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notes.Api.Extensions;
using Notes.Application;
using Notes.Infrastructure;
using Notes.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Composition root: each layer owns its own registrations.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Notes API v1");
        options.DocumentTitle = "Notes API";
    });
}

app.UseCors(ServiceCollectionExtensions.CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(
            new
            {
                status = report.Status == HealthStatus.Healthy ? "ok" : report.Status.ToString().ToLowerInvariant(),
                timestamp = DateTime.UtcNow,
                checks = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.Status.ToString()),
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    },
}).AllowAnonymous();

// Create the database and apply the schema before serving traffic.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

app.Run();

/// <summary>Exposed so the integration tests can spin the real pipeline up in-process.</summary>
public partial class Program;
