using MassTransit;
using Microsoft.IdentityModel.Logging;
using Play.Common.Configuration;
using Play.Common.HealthChecks;
using Play.Common.Identity;
using Play.Common.Logging;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Common.OpenTelemetry;
using Play.Inventory.Services.Clients;
using Play.Inventory.Services.Entities;
using Play.Inventory.Services.Exceptions;
using Polly;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

IdentityModelEventSource.ShowPII = true;
const string AllowedOriginSetting = "AllowedOrigin";

builder.Host.ConfigureAzureKeyVault();

builder.Services.AddMongo()
                .AddMongoRepository<InventoryItem>("inventoryitems")
                .AddMongoRepository<CatalogItem>("catalogitems")
                .AddMassTransitWithMessageBroker(builder.Configuration, retryConfigurator =>
                {
                    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                    retryConfigurator.Ignore(typeof(UnknownItemException));
                })
                .AddJwtBearerAuthentication();

AddCatalogClient(builder.Services);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
                .AddMongoDb();

builder.Services.AddSeqLogging(builder.Configuration)
                .AddTracing(builder.Configuration)
                .AddMetrics(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(corsBuilder =>
    {
        corsBuilder.WithOrigins(builder.Configuration[AllowedOriginSetting])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapPlayEconomyHealthChecks();

app.Run();


void AddCatalogClient(IServiceCollection services)
{
    Random jitterer = new Random();

    services.AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:5001");
    })
    .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
        5,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(1, retryAttempt))
                        + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000))
    ))
    .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
        3,
        TimeSpan.FromSeconds(15)
    ))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
}