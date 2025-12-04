

using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace MicroShop.Shared.Extensions;

public static class LoggingExtensions
{
    public static void AddCustomLogging(this WebApplicationBuilder builder, string applicationName)
    {
        var elasticUri = new Uri(builder.Configuration["ElasticConfiguration:Uri"] ?? "http://localhost:9200");

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", applicationName)
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(elasticUri)
            {
                // Index formatı küçük harf olmalı. Örn: microshop-orderapi-2025-12
                IndexFormat = $"microshop-{applicationName.ToLower()}-{DateTime.UtcNow:yyyy-MM}",
                AutoRegisterTemplate = false, 
                NumberOfShards = 2,
                NumberOfReplicas = 1
            })
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}