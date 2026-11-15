using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler
    : IRequestHandler<UpdateProductCommand>
{
    private readonly IInventoryData _data;
    private readonly IValidator<ProductRequest> _validator;
    private readonly ILogger<UpdateProductHandler> _logger;

    public UpdateProductHandler(
        IInventoryData data,
        IValidator<ProductRequest> validator,
        ILogger<UpdateProductHandler> logger)
    {
        _data = data;
        _validator = validator;
        _logger = logger;
    }

    public async Task Handle(
        UpdateProductCommand command,
        CancellationToken ct)
    {
        var request = new ProductRequest
        {
            Name = command.Name,
            SKU = command.SKU,
            Description = command.Description
        };

        await _validator.ValidateAndThrowAsync(request, ct);

        var product = await _data.FindAsync<Product>(ct, command.Id)
            ?? throw new AppException(404, "Product not found.");

        var sku = command.SKU.Trim().ToUpperInvariant();

        if (await _data.ProductSkuExistsAsync(sku, command.Id, ct))
        {
            throw new AppException(409, "SKU already exists.");
        }

        product.Name = command.Name.Trim();
        product.SKU = sku;
        product.NormalizedSku = sku;

        product.Description =
            string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim();

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Product {ProductId} updated with SKU {SKU}",
            product.Id,
            product.SKU);
    }
}
