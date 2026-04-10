using Application.Services;
using Contracts.Requests;
using Contracts.Responses;
using MediatR;

namespace Application.Handlers;

public record GetTransactionsQuery(GetTransactionsRequest Request) : IRequest<PagedResponse<TransactionResponse>>;

public class GetTransactionsHandler(ITransactionRepository repository)
    : IRequestHandler<GetTransactionsQuery, PagedResponse<TransactionResponse>>
{
    public async Task<PagedResponse<TransactionResponse>> Handle(GetTransactionsQuery request, CancellationToken ct)
    {
        return await repository.GetPagedAsync(request.Request, ct);
    }
}