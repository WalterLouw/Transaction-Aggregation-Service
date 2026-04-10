using System.Net;
using System.Text.Json;
using Domain.Exceptions;

namespace transaction_aggregator.Exceptions;

public class ExceptionHandling(
    RequestDelegate next,
    ILogger<ExceptionHandling> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            TransactionNotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
            TransactionDomainException ex => (HttpStatusCode.BadRequest, ex.Message),
            IngestionException ex => (HttpStatusCode.BadGateway, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occured")
        };
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            error = message
        });
        
        await context.Response.WriteAsync(payload);
    }
}