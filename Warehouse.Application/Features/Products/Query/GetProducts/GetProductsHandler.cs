using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Products.GetProducts;

public sealed class GetProductsHandler
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    private readonly IInventoryData _data;

    public GetProductsHandler(IInventoryData data)
    {
        _data = data;
    }

    public Task<IReadOnlyList<ProductResponse>> Handle(
        GetProductsQuery query,
        CancellationToken ct)
    {
        return _data.GetProductsAsync(ct);
    }
}
