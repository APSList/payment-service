using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using payment_service;
using payment_service.Database;
using payment_service.Interfaces;
using payment_service.Options;
using payment_service.Services;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer(); // za minimal API in controller explorer
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
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

builder.Services.AddHttpContextAccessor();

//Dependency injeciton
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStripeIntegrationService, StripeIntegrationService>();
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
builder.Services.AddScoped<IPaymentConfirmationService, PaymentConfirmationService>();
builder.Services.AddScoped<IOrganizationContext, OrganizationContext>();

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

// Supabase issuer
var issuer = "https://frauwrkbphmjngymcdyk.supabase.co/auth/v1";

// vzame OpenID configuration (vključno z jwks_uri) in cache-a ključe sam.
// Supabase podpira .well-known/openid-configuration na auth/v1.

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.RequireHttpsMetadata = true;

        // Audience (pri Supabase tipično "authenticated")
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = "authenticated",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5), // leeway kot v Go

            // enforce alg ES256 (točno kot WithValidMethods)
            ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 },

            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        // Explicitno nastavimo ConfigurationManager, da je jasno da gre čez .well-known + jwks caching
        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{issuer}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever()
        );

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity is null)
                {
                    context.Fail("Missing identity.");
                    return Task.CompletedTask;
                }

                var userMetadataJson = context.Principal!.FindFirst("user_metadata")?.Value;
                if (string.IsNullOrWhiteSpace(userMetadataJson))
                {
                    context.Fail("Missing user_metadata.");
                    return Task.CompletedTask;
                }

                try
                {
                    using var doc = JsonDocument.Parse(userMetadataJson);

                    if (!doc.RootElement.TryGetProperty("organization_id", out var orgEl))
                    {
                        context.Fail("Missing user_metadata.organization_id.");
                        return Task.CompletedTask;
                    }

                    int? orgId =
                        orgEl.ValueKind == JsonValueKind.Number ? orgEl.GetInt32() :
                        orgEl.ValueKind == JsonValueKind.String && int.TryParse(orgEl.GetString(), out var parsed) ? parsed :
                        (int?)null;

                    if (orgId is null || orgId <= 0)
                    {
                        context.Fail("Invalid organization_id.");
                        return Task.CompletedTask;
                    }

                    // kot gin.Context.Set("organization_id", ...)
                    identity.AddClaim(new Claim("organization_id", orgId.Value.ToString()));

                    // role iz user_metadata.role (opcijsko)
                    if (doc.RootElement.TryGetProperty("role", out var roleEl) &&
                        roleEl.ValueKind == JsonValueKind.String)
                    {
                        var role = roleEl.GetString();
                        if (!string.IsNullOrWhiteSpace(role))
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }

                    return Task.CompletedTask;
                }
                catch (JsonException)
                {
                    context.Fail("Invalid user_metadata JSON.");
                    return Task.CompletedTask;
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Enako kot v Go: “mora obstajati organization_id”
    options.AddPolicy("OrgRequired", policy => policy.RequireClaim("organization_id"));

    // Če želiš da je auth privzeto obvezen povsod:
    // options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    //     .RequireAuthenticatedUser()
    //     .Build();
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

var app = builder.Build();

var cfgPrefix = builder.Configuration["SwaggerPrefix"];



if (!string.IsNullOrEmpty(cfgPrefix))
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            var basePath = httpReq.PathBase.Value;
            swaggerDoc.Servers = new List<OpenApiServer>
            {
                new() { Url = $"https://{httpReq.Host}{cfgPrefix}" }
            };
        });


    });
}
else
{
    app.UseSwagger();
}

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

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowAngularDev");

app.MapControllers();

app.Run();
