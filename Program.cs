using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
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

builder.Services
    .AddOptions<KafkaOptions>()
    .Bind(builder.Configuration.GetSection(KafkaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

//Dependency injeciton
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStripeIntegrationService, StripeIntegrationService>();
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
builder.Services.AddScoped<IPaymentConfirmationService, PaymentConfirmationService>();


builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

var kafkaOptions = builder.Configuration
    .GetSection(KafkaOptions.SectionName)
    .Get<KafkaOptions>();

if (kafkaOptions!.EnableKafka == true)
{
    builder.Services.AddHostedService<OutboxPublisherService>();
}

//Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Supabase")));

//Logging
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning) // Manjši šum
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "payment-service")
        .WriteTo.Console(new RenderedCompactJsonFormatter());
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

app.UseSwagger(c =>
{
    if (app.Environment.IsDevelopment())
    {
        var cfgPrefix = builder.Configuration["SwaggerPrefix"];

        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            var basePath = httpReq.PathBase.Value;
            swaggerDoc.Servers = new List<OpenApiServer>
        {
            new() { Url = $"{httpReq.Scheme}://{httpReq.Host}{cfgPrefix}" }
        };
        });
    }
});

app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
    c.SwaggerEndpoint("./v1/swagger.json", "payment-service v1");
});



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
