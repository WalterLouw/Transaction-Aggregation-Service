namespace Domain.Exceptions;

public class TransactionDomainException(string message)
    : Exception(message);
 
public class TransactionNotFoundException(Guid id)
    : Exception($"Transaction '{id}' was not found.");
 
public class IngestionException(string sourceId, string message, Exception? inner = null)
    : Exception($"Ingestion failed for source '{sourceId}': {message}", inner);