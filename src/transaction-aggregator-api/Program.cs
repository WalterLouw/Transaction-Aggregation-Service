using Application.Handlers;
using Contracts.Responses;
using Hangfire;
using Infrastructure;
using Infrastructure.Jobs;
using MediatR;
using transaction_aggregator.Exceptions;
using transaction_aggregator.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddApiVersioningConfig()
    .AddSwaggerConfig()
    .AddControllers();

builder.Services.AddOpenApi("v1");

var app = builder.Build();

app.UseMiddleware<ExceptionHandling>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Hangfire dashboard — available at /hangfire in development only
    app.UseHangfireDashboard("/hangfire");
}

// Register the recurring aggregation job using the cron from config
var hangfireOptions = app.Services.GetRequiredService<HangfireOptions>();
RecurringJob.AddOrUpdate<AggregationJob>(
    recurringJobId: "aggregation-run",
    methodCall: job => job.RunAsync(CancellationToken.None),
    cronExpression: hangfireOptions.AggregationCron);

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
