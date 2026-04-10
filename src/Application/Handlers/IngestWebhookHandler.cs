using Application.Services;
using Contracts.Requests;
using Contracts.Responses;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Handlers;

public record IngestWebhookCommand(IngestWebhookRequest Request) : IRequest<WebhookAcceptedResponse>;

public class IngestWebhookHandler(
    ITransactionRepository repository,
    IDeduplicationService deduplicationService,
    ILogger<IngestWebhookHandler> logger)
    : IRequestHandler<IngestWebhookCommand, WebhookAcceptedResponse>
{
    public async Task<WebhookAcceptedResponse> Handle(IngestWebhookCommand request, CancellationToken ct)
    {
        var req = request.Request;
        //Duplicate check
        if (await deduplicationService.IsDuplicateAsync(req.ExternalId, req.SourceId, ct))
        {
            logger.LogInformation("Duplicate webhook ignored: {ExternalId} from {SourceId}", req.ExternalId, req.SourceId);
            
            return new WebhookAcceptedResponse()
            {
                Id = Guid.Empty,
                Status = "Duplicate"
            };
        }
        
        var transaction = Transaction.Create(
            externalId: req.ExternalId,
            sourceId: req.SourceId,
            amount: req.Amount,
            currency: req.Currency,
            occurredAt: req.OccurredAt,
            metadata: req.Metadata
        );
 
        transaction.MarkProcessed();
        
        await repository.SaveAsync(transaction, ct);
        await deduplicationService.MarkAsSeenAsync(req.ExternalId, req.SourceId, ct);
        
        logger.LogInformation("Webhook ingested: {Id} from {SourceId}", transaction.Id, req.SourceId);
        
        return new WebhookAcceptedResponse()
        {
            Id = transaction.Id,
            Status = "Accepted"
        };

    }
}