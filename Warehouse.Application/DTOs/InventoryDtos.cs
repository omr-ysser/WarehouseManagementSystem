using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

// Warehouses

public sealed class WarehouseRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed record WarehouseResponse(
    int Id,
    string Name);

// Products

public sealed class ProductRequest
{
    public string Name { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public sealed record ProductResponse(
    int Id,
    string Name,
    string SKU,
    string? Description);

// Transactions

public class CreateStockTransactionRequest
{
    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public decimal Quantity { get; set; }
}

public sealed class UpdateStockTransactionRequest
    : CreateStockTransactionRequest
{
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class DeleteStockTransactionRequest
{
    public string RowVersion { get; set; } = string.Empty;
}

public sealed record StockTransactionResponse(
    int Id,
    int WarehouseId,
    int ProductId,
    decimal Quantity,
    DateTimeOffset CreatedAtUtc,
    string CreatedByUserId,
    string RowVersion);

// Available stock

public sealed record StockResponse(
    int WarehouseId,
    int ProductId,
    string ProductName,
    string SKU,
    decimal Quantity);
