namespace Infrastructure.Jobs;

public class HangfireOptions
{
    public const string SectionName = "Hangfire";
 
    /// <summary>
    /// Cron expression for the aggregation job schedule.
    /// Default: every 10 minutes.
    /// Examples:
    ///   "*/10 * * * *"  — every 10 minutes
    ///   "*/5 * * * *"   — every 5 minutes
    ///   "0 * * * *"     — every hour
    /// </summary>
    public string AggregationCron { get; init; } = "*/10 * * * *";
 
    //Number of Hangfire background processing workers.
    public int WorkerCount { get; init; } = 2;
}