using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Logging;
using Play.Catalog.Services;
using Play.Catalog.Services.Entities;
using Play.Common.Configuration;
using Play.Common.HealthChecks;
using Play.Common.Identity;
using Play.Common.Logging;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Common.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

IdentityModelEventSource.ShowPII = true;
const string AllowedOriginSetting = "AllowedOrigin";

builder.Host.ConfigureAzureKeyVault();

builder.Services.AddMongo()
                .AddMongoRepository<Item>("items")
                .AddMassTransitWithMessageBroker(builder.Configuration)
                .AddJwtBearerAuthentication();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Read, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "catalog.readaccess", "catalog.fullaccess");
    });

    options.AddPolicy(Policies.Write, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "catalog.writeaccess", "catalog.fullaccess");
    });
});

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

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
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();

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
