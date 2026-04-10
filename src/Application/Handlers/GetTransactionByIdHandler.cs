using Application.Mapping;
using Application.Services;
using Contracts.Responses;
using Domain.Exceptions;
using MediatR;

namespace Application.Handlers;

public record GetTransactionByIdQuery(Guid Id) : IRequest<TransactionResponse>;

public class GetTransactionByIdHandler(ITransactionRepository repository)
    : IRequestHandler<GetTransactionByIdQuery, TransactionResponse>
{
    public async Task<TransactionResponse> Handle(GetTransactionByIdQuery request, CancellationToken ct)
    {
        var transaction = await repository.GetByIdAsync(request.Id, ct)
            ?? throw new TransactionNotFoundException(request.Id);
        
        return TransactionMapper.ToResponse(transaction);
    }
}