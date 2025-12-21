using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using payment_service.Database;
using payment_service.Interfaces;
using payment_service.Options;
using payment_service.Services;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer(); // za minimal API in controller explorer
builder.Services.AddSwaggerGen();

//Options
builder.Services
    .AddOptions<StripeOptions>()
    .Bind(builder.Configuration.GetSection(StripeOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(opt => !string.IsNullOrEmpty(opt.ApiKey), "ApiKey should not be empty")
    .ValidateOnStart();

//Dependency injeciton
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStripeIntegrationService, StripeIntegrationService>();
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
builder.Services.AddScoped<IPaymentConfirmationService, PaymentConfirmationService>();

//Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Supabase")));

//Logging
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "payment-service")
        .WriteTo.Console()
        .WriteTo.Http(
            requestUri: "http://localhost:5044", // lokalni Logstash
            queueLimitBytes: null,
            textFormatter: new RenderedCompactJsonFormatter()
        );
});

//Health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Supabase") ?? "",
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Name == "self"
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});

app.MapGet("/ok", () =>
{
    Log.Information("OK endpoint called");
    return "OK";
});

app.MapGet("/error", () =>
{
    Log.Error("Something went wrong");
    return Results.Problem("Error");
});

app.UseHttpMetrics();     // meri HTTP odzivnost, status kode, metode
app.MapMetrics();         // /metrics endpoint

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
