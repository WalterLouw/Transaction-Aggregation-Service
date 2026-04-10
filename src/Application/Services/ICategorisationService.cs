using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Determines the category of a transaction
/// </summary>
public interface ICategorisationService
{
    TransactionCategory Categorise(Transaction transaction);
}