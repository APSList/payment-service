using Microsoft.EntityFrameworkCore;
using Npgsql;
using payment_service.Database;
using payment_service.Interfaces;
using payment_service.Options;
using payment_service.Services;

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


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API v1");
            options.RoutePrefix = string.Empty; // UI na root
        });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
