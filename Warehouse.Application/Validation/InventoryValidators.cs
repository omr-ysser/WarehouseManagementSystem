using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Warehouse.Application.DTOs;

using FluentValidation;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Validation;

public static class InventoryLimits
{
    public const decimal MaxQuantity = 999999999999999.999m;

    public static bool IsValidQuantity(decimal quantity)
    {
        return quantity != 0
            && quantity >= -MaxQuantity
            && quantity <= MaxQuantity
            && decimal.Round(quantity, 3) == quantity;
    }

    public static bool IsValidRowVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(value).Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool HasTrimmedLength(
        string? value,
        int minimum,
        int maximum)
    {
        if (value is null)
        {
            return false;
        }

        var length = value.Trim().Length;

        return length >= minimum && length <= maximum;
    }
}

public sealed class WarehouseRequestValidator
    : AbstractValidator<WarehouseRequest>
{
    public WarehouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(value =>
                InventoryLimits.HasTrimmedLength(value, 2, 100))
            .WithMessage(
                "Warehouse name must contain 2–100 characters after trimming.");
    }
}

public sealed class ProductRequestValidator
    : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(value =>
                InventoryLimits.HasTrimmedLength(value, 2, 150))
            .WithMessage(
                "Product name must contain 2–150 characters after trimming.");

        RuleFor(x => x.SKU)
            .Must(value =>
                InventoryLimits.HasTrimmedLength(value, 2, 50))
            .WithMessage(
                "SKU must contain 2–50 characters after trimming.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage(
                "Description must not exceed 1000 characters.");
    }
}

public sealed class CreateStockTransactionRequestValidator
    : AbstractValidator<CreateStockTransactionRequest>
{
    public CreateStockTransactionRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0)
            .WithMessage(
                "WarehouseId must be greater than zero.");

        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .WithMessage(
                "ProductId must be greater than zero.");

        RuleFor(x => x.Quantity)
            .Must(InventoryLimits.IsValidQuantity)
            .WithMessage(
                "Quantity must be nonzero, fit decimal(18,3), " +
                "and contain no more than 3 decimal places.");
    }
}

public sealed class UpdateStockTransactionRequestValidator
    : AbstractValidator<UpdateStockTransactionRequest>
{
    public UpdateStockTransactionRequestValidator()
    {
        Include(new CreateStockTransactionRequestValidator());

        RuleFor(x => x.RowVersion)
            .Must(InventoryLimits.IsValidRowVersion)
            .WithMessage(
                "A valid 8-byte Base64 RowVersion is required.");
    }
}

public sealed class DeleteStockTransactionRequestValidator
    : AbstractValidator<DeleteStockTransactionRequest>
{
    public DeleteStockTransactionRequestValidator()
    {
        RuleFor(x => x.RowVersion)
            .Must(InventoryLimits.IsValidRowVersion)
            .WithMessage(
                "A valid 8-byte Base64 RowVersion is required.");
    }
}