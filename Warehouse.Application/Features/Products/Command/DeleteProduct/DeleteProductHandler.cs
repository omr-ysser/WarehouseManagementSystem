using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Microsoft.Extensions.Logging;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler
    : IRequestHandler<DeleteProductCommand>
{
    private readonly IInventoryData _data;
    private readonly ILogger<DeleteProductHandler> _logger;

    public DeleteProductHandler(
        IInventoryData data,
        ILogger<DeleteProductHandler> logger)
    {
        _data = data;
        _logger = logger;
    }

    public async Task Handle(
        DeleteProductCommand command,
        CancellationToken ct)
    {
        var product = await _data.FindAsync<Product>(ct, command.Id)
            ?? throw new AppException(404, "Product not found.");

        if (await _data.ProductHasDependenciesAsync(command.Id, ct))
        {
            throw new AppException(
                409,
                "A product with stock rows or transactions cannot be deleted.");
        }

        _data.Remove(product);

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Product {ProductId} deleted",
            command.Id);
    }
}
