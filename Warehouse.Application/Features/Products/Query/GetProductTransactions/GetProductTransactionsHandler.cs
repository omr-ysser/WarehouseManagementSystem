using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Products.GetProductTransactions;

public sealed class GetProductTransactionsHandler
    : IRequestHandler<
        GetProductTransactionsQuery,
        IReadOnlyList<StockTransactionResponse>>
{
    private readonly IInventoryData _data;

    public GetProductTransactionsHandler(IInventoryData data)
    {
        _data = data;
    }

    public async Task<IReadOnlyList<StockTransactionResponse>> Handle(
        GetProductTransactionsQuery query,
        CancellationToken ct)
    {
        var product = await _data.FindAsync<Product>(
            ct,
            query.ProductId);

        if (product is null)
        {
            throw new AppException(404, "Product not found.");
        }

        return await _data.GetTransactionsAsync(
            null,
            query.ProductId,
            ct);
    }
}
