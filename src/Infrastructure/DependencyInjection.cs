using Application.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Categorisation;
using Infrastructure.Categorisation.Options;
using Infrastructure.Deduplication;
using Infrastructure.ExternalApi;
using Infrastructure.ExternalApi.Options;
using Infrastructure.Sql;
using Infrastructure.Jobs;
using Infrastructure.Repositories;
using Infrastructure.TransactionDb;
using Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        //Persistence
        services.AddDbContext<TransactionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("Postgres")!);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
 
        services.AddDbContext<TransactionDbContext>(options =>
            options.UseNpgsql(dataSource));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IDeduplicationService, DeduplicationService>();
        
        //Categorisation
        var categorisationOptions = configuration
            .GetSection(CategorisationOptions.SectionName)
            .Get<CategorisationOptions>() ?? new CategorisationOptions();
        
        services.AddSingleton(categorisationOptions);
        services.AddSingleton<ICategorisationService, CategorisationService>();

        //Aggregation Pipeline
        services.AddScoped<IAggregationService, AggregationService.AggregationService>();
        services.AddScoped<AggregationJob>();
        
        //SQL Source
        services.AddScoped<ITransactionSource>(_ =>
            new MssqlTransactionSource(
                configuration.GetConnectionString("MssqlPrimary")!,
                _.GetRequiredService<ILogger<MssqlTransactionSource>>()));
        
        //External API source
        var externalApiOptions = configuration.GetSection(ExternalApiOptions.SectionName)
            .Get<ExternalApiOptions>() ?? new ExternalApiOptions();
        
        services.AddSingleton(externalApiOptions);
        
        services.AddHttpClient<ITransactionSource, ExternalTransactionSource>()
            .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
        
        // Webhooks
        var webhookOptions = configuration
            .GetSection(WebhookOptions.SectionName)
            .Get<WebhookOptions>() ?? new WebhookOptions();
 
        services.AddSingleton(webhookOptions);
        services.AddSingleton<IWebhookSignatureValidator, HmacSha512SignatureValidator>();
        
        //Hangfire
        var hangfireOptions = configuration.GetSection("Hangfire").Get<HangfireOptions>() ?? new HangfireOptions();

        services.AddSingleton(hangfireOptions);

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(configuration.GetConnectionString("Postgres")!)));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireOptions.WorkerCount; 
            options.Queues = ["default"];
        });

        return services;
    }
}