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

namespace Warehouse.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler
    : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IInventoryData _data;
    private readonly IValidator<ProductRequest> _validator;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(
        IInventoryData data,
        IValidator<ProductRequest> validator,
        ILogger<CreateProductHandler> logger)
    {
        _data = data;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ProductResponse> Handle(
        CreateProductCommand command,
        CancellationToken ct)
    {
        var request = new ProductRequest
        {
            Name = command.Name,
            SKU = command.SKU,
            Description = command.Description
        };

        await _validator.ValidateAndThrowAsync(request, ct);

        var sku = command.SKU.Trim().ToUpperInvariant();

        if (await _data.ProductSkuExistsAsync(sku, null, ct))
        {
            throw new AppException(409, "SKU already exists.");
        }

        var product = new Product
        {
            Name = command.Name.Trim(),
            SKU = sku,
            NormalizedSku = sku,
            Description =
                string.IsNullOrWhiteSpace(command.Description)
                    ? null
                    : command.Description.Trim()
        };

        _data.Add(product);

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Product {ProductId} created with SKU {SKU}",
            product.Id,
            product.SKU);

        return new ProductResponse(
            product.Id,
            product.Name,
            product.SKU,
            product.Description);
    }
}
