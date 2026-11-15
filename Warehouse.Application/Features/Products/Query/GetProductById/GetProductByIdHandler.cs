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

namespace Warehouse.Application.Features.Products.GetProductById;

public sealed class GetProductByIdHandler
    : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    private readonly IInventoryData _data;

    public GetProductByIdHandler(IInventoryData data)
    {
        _data = data;
    }

    public async Task<ProductResponse> Handle(
        GetProductByIdQuery query,
        CancellationToken ct)
    {
        var product = await _data.FindAsync<Product>(ct, query.Id)
            ?? throw new AppException(404, "Product not found.");

        return new ProductResponse(
            product.Id,
            product.Name,
            product.SKU,
            product.Description);
    }
}
