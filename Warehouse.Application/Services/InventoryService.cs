using FluentValidation;
using Microsoft.Extensions.Logging;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Application.Validation;
using Warehouse.Domain.Entities;
using WarehouseEntity = Warehouse.Domain.Entities.Warehouse;

namespace Warehouse.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryData _data;
    private readonly ILogger<InventoryService> _logger;

    private readonly IValidator<WarehouseRequest> _warehouseValidator;
    private readonly IValidator<ProductRequest> _productValidator;

    private readonly IValidator<CreateStockTransactionRequest>
        _createTransactionValidator;

    private readonly IValidator<UpdateStockTransactionRequest>
        _updateTransactionValidator;

    private readonly IValidator<DeleteStockTransactionRequest>
        _deleteTransactionValidator;

    public InventoryService(
        IInventoryData data,
        IValidator<WarehouseRequest> warehouseValidator,
        IValidator<ProductRequest> productValidator,
        IValidator<CreateStockTransactionRequest> createTransactionValidator,
        IValidator<UpdateStockTransactionRequest> updateTransactionValidator,
        IValidator<DeleteStockTransactionRequest> deleteTransactionValidator,
        ILogger<InventoryService> logger)
    {
        _data = data;
        _warehouseValidator = warehouseValidator;
        _productValidator = productValidator;
        _createTransactionValidator = createTransactionValidator;
        _updateTransactionValidator = updateTransactionValidator;
        _deleteTransactionValidator = deleteTransactionValidator;
        _logger = logger;
    }

    private async Task<T> RequireAsync<T>(
        int id,
        string resource,
        CancellationToken ct)
        where T : class
    {
        return await _data.FindAsync<T>(ct, id)
            ?? throw new AppException(
                404,
                $"{resource} not found.");
    }

    private static ProductResponse Map(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.SKU,
            product.Description);
    }

    private static StockTransactionResponse Map(
        StockTransaction transaction)
    {
        return new StockTransactionResponse(
            transaction.Id,
            transaction.WarehouseId,
            transaction.ProductId,
            transaction.Quantity,
            transaction.CreatedAtUtc,
            transaction.CreatedByUserId,
            Convert.ToBase64String(transaction.RowVersion));
    }

    private static void EnsureVersion(
        StockTransaction transaction,
        string rowVersion)
    {
        var expected = Convert.FromBase64String(rowVersion);

        if (!transaction.RowVersion.SequenceEqual(expected))
        {
            throw new AppException(
                409,
                "The transaction changed. Reload it before editing or deleting.");
        }
    }

    // Warehouses

    public Task<IReadOnlyList<WarehouseResponse>> GetWarehousesAsync(
        CancellationToken ct)
    {
        return _data.GetWarehousesAsync(ct);
    }

    public async Task<WarehouseResponse> GetWarehouseAsync(
        int id,
        CancellationToken ct)
    {
        var warehouse = await RequireAsync<WarehouseEntity>(
            id,
            "Warehouse",
            ct);

        return new WarehouseResponse(
            warehouse.Id,
            warehouse.Name);
    }

    public async Task<WarehouseResponse> CreateWarehouseAsync(
        WarehouseRequest request,
        CancellationToken ct)
    {
        await _warehouseValidator.ValidateAndThrowAsync(request, ct);

        var name = request.Name.Trim();
        var normalized = name.ToUpperInvariant();

        if (await _data.WarehouseNameExistsAsync(
                normalized, null, ct))
        {
            throw new AppException(
                409,
                "Warehouse name already exists.");
        }

        var warehouse = new WarehouseEntity
        {
            Name = name,
            NormalizedName = normalized
        };

        _data.Add(warehouse);

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Warehouse {WarehouseId} created with name {WarehouseName}",
            warehouse.Id,
            warehouse.Name);

        return new WarehouseResponse(
            warehouse.Id,
            warehouse.Name);
    }

    public async Task UpdateWarehouseAsync(
        int id,
        WarehouseRequest request,
        CancellationToken ct)
    {
        await _warehouseValidator.ValidateAndThrowAsync(request, ct);

        var warehouse = await RequireAsync<WarehouseEntity>(
            id,
            "Warehouse",
            ct);

        var name = request.Name.Trim();
        var normalized = name.ToUpperInvariant();

        if (await _data.WarehouseNameExistsAsync(
                normalized, id, ct))
        {
            throw new AppException(
                409,
                "Warehouse name already exists.");
        }

        warehouse.Name = name;
        warehouse.NormalizedName = normalized;

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Warehouse {WarehouseId} updated",
            id);
    }

    public async Task DeleteWarehouseAsync(
        int id,
        CancellationToken ct)
    {
        var warehouse = await RequireAsync<WarehouseEntity>(
            id,
            "Warehouse",
            ct);

        if (await _data.WarehouseHasDependenciesAsync(id, ct))
        {
            throw new AppException(
                409,
                "A warehouse with stock rows or transactions cannot be deleted.");
        }

        _data.Remove(warehouse);

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Warehouse {WarehouseId} deleted",
            id);
    }

    // Products

    public Task<IReadOnlyList<ProductResponse>> GetProductsAsync(
        CancellationToken ct)
    {
        return _data.GetProductsAsync(ct);
    }

    public async Task<ProductResponse> GetProductAsync(
        int id,
        CancellationToken ct)
    {
        return Map(
            await RequireAsync<Product>(id, "Product", ct));
    }

    public async Task<ProductResponse> CreateProductAsync(
        ProductRequest request,
        CancellationToken ct)
    {
        await _productValidator.ValidateAndThrowAsync(request, ct);

        var sku = request.SKU.Trim().ToUpperInvariant();

        if (await _data.ProductSkuExistsAsync(sku, null, ct))
        {
            throw new AppException(
                409,
                "SKU already exists.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            SKU = sku,
            NormalizedSku = sku,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim()
        };

        _data.Add(product);

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Product {ProductId} created with SKU {SKU} and name {ProductName}",
            product.Id,
            product.SKU,
            product.Name);

        return Map(product);
    }

    public async Task UpdateProductAsync(
        int id,
        ProductRequest request,
        CancellationToken ct)
    {
        await _productValidator.ValidateAndThrowAsync(request, ct);

        var product = await RequireAsync<Product>(
            id,
            "Product",
            ct);

        var sku = request.SKU.Trim().ToUpperInvariant();

        if (await _data.ProductSkuExistsAsync(sku, id, ct))
        {
            throw new AppException(
                409,
                "SKU already exists.");
        }

        product.Name = request.Name.Trim();
        product.SKU = sku;
        product.NormalizedSku = sku;

        product.Description =
            string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Product {ProductId} updated with SKU {SKU}",
            product.Id,
            product.SKU);
    }

    public async Task DeleteProductAsync(
        int id,
        CancellationToken ct)
    {
        var product = await RequireAsync<Product>(
            id,
            "Product",
            ct);

        if (await _data.ProductHasDependenciesAsync(id, ct))
        {
            throw new AppException(
                409,
                "A product with stock rows or transactions cannot be deleted.");
        }

        _data.Remove(product);

        await _data.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Product {ProductId} deleted",
            id);
    }

    // Stock transactions

    public async Task<IReadOnlyList<StockTransactionResponse>>
        GetTransactionsAsync(
            int? warehouseId,
            int? productId,
            CancellationToken ct)
    {
        if (warehouseId.HasValue)
        {
            await RequireAsync<WarehouseEntity>(
                warehouseId.Value,
                "Warehouse",
                ct);
        }

        if (productId.HasValue)
        {
            await RequireAsync<Product>(
                productId.Value,
                "Product",
                ct);
        }

        return await _data.GetTransactionsAsync(
            warehouseId,
            productId,
            ct);
    }

    public async Task<StockTransactionResponse> GetTransactionAsync(
        int id,
        CancellationToken ct)
    {
        return Map(
            await RequireAsync<StockTransaction>(
                id,
                "Transaction",
                ct));
    }

    private async Task ValidateReferencesAsync(
        int warehouseId,
        int productId,
        CancellationToken ct)
    {
        await RequireAsync<WarehouseEntity>(
            warehouseId,
            "Warehouse",
            ct);

        await RequireAsync<Product>(
            productId,
            "Product",
            ct);
    }

    private async Task ApplyBalanceChangesAsync(
        IEnumerable<(int WarehouseId, int ProductId, decimal Delta)> changes,
        CancellationToken ct)
    {
        var grouped = changes
            .GroupBy(x => new
            {
                x.WarehouseId,
                x.ProductId
            })
            .Select(x => new
            {
                x.Key.WarehouseId,
                x.Key.ProductId,
                Delta = x.Sum(item => item.Delta)
            })
            .OrderBy(x => x.WarehouseId)
            .ThenBy(x => x.ProductId);

        foreach (var change in grouped)
        {
            if (change.Delta == 0)
            {
                continue;
            }

            var stock = await _data.FindAsync<WarehouseStock>(
                ct,
                change.WarehouseId,
                change.ProductId);

            var current = stock?.Quantity ?? 0m;
            var updated = current + change.Delta;

            if (updated < 0)
            {
                throw new AppException(
                    400,
                    $"Insufficient stock for product {change.ProductId} " +
                    $"in warehouse {change.WarehouseId}.");
            }

            if (updated > InventoryLimits.MaxQuantity)
            {
                throw new AppException(
                    400,
                    "The resulting stock exceeds the supported quantity.");
            }

            if (stock is null)
            {
                stock = new WarehouseStock
                {
                    WarehouseId = change.WarehouseId,
                    ProductId = change.ProductId,
                    Quantity = updated
                };

                _data.Add(stock);
            }
            else
            {
                stock.Quantity = updated;
            }
        }
    }

    public async Task<StockTransactionResponse> CreateTransactionAsync(
        CreateStockTransactionRequest request,
        string userId,
        CancellationToken ct)
    {
        await _createTransactionValidator.ValidateAndThrowAsync(
            request,
            ct);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new AppException(
                401,
                "A valid user is required.");
        }

        await using var transaction =
            await _data.BeginTransactionAsync(ct);

        await ValidateReferencesAsync(
            request.WarehouseId,
            request.ProductId,
            ct);

        await ApplyBalanceChangesAsync(
            new[]
            {
                (
                    request.WarehouseId,
                    request.ProductId,
                    request.Quantity
                )
            },
            ct);

        var entity = new StockTransaction
        {
            WarehouseId = request.WarehouseId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        _data.Add(entity);

        await _data.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation(
            "Stock transaction {TransactionId} created for warehouse " +
            "{WarehouseId}, product {ProductId}, quantity {Quantity}, " +
            "by user {UserId}",
            entity.Id,
            entity.WarehouseId,
            entity.ProductId,
            entity.Quantity,
            userId);

        return Map(entity);
    }

    public async Task<StockTransactionResponse> UpdateTransactionAsync(
        int id,
        UpdateStockTransactionRequest request,
        CancellationToken ct)
    {
        await _updateTransactionValidator.ValidateAndThrowAsync(
            request,
            ct);

        await using var transaction =
            await _data.BeginTransactionAsync(ct);

        var entity = await RequireAsync<StockTransaction>(
            id,
            "Transaction",
            ct);

        EnsureVersion(entity, request.RowVersion);

        await ValidateReferencesAsync(
            request.WarehouseId,
            request.ProductId,
            ct);

        var previousWarehouseId = entity.WarehouseId;
        var previousProductId = entity.ProductId;
        var previousQuantity = entity.Quantity;

        await ApplyBalanceChangesAsync(
            new[]
            {
                (
                    previousWarehouseId,
                    previousProductId,
                    -previousQuantity
                ),
                (
                    request.WarehouseId,
                    request.ProductId,
                    request.Quantity
                )
            },
            ct);

        entity.WarehouseId = request.WarehouseId;
        entity.ProductId = request.ProductId;
        entity.Quantity = request.Quantity;

        await _data.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation(
            "Stock transaction {TransactionId} updated: " +
            "warehouse {PreviousWarehouseId} to {WarehouseId}, " +
            "product {PreviousProductId} to {ProductId}, " +
            "quantity {PreviousQuantity} to {Quantity}",
            entity.Id,
            previousWarehouseId,
            entity.WarehouseId,
            previousProductId,
            entity.ProductId,
            previousQuantity,
            entity.Quantity);

        return Map(entity);
    }

    public async Task DeleteTransactionAsync(
        int id,
        DeleteStockTransactionRequest request,
        CancellationToken ct)
    {
        await _deleteTransactionValidator.ValidateAndThrowAsync(
            request,
            ct);

        await using var transaction =
            await _data.BeginTransactionAsync(ct);

        var entity = await RequireAsync<StockTransaction>(
            id,
            "Transaction",
            ct);

        EnsureVersion(entity, request.RowVersion);

        await ApplyBalanceChangesAsync(
            new[]
            {
                (
                    entity.WarehouseId,
                    entity.ProductId,
                    -entity.Quantity
                )
            },
            ct);

        _data.Remove(entity);

        await _data.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation(
            "Stock transaction {TransactionId} deleted for warehouse " +
            "{WarehouseId}, product {ProductId}, quantity {Quantity}",
            id,
            entity.WarehouseId,
            entity.ProductId,
            entity.Quantity);
    }

    // Available stock

    public async Task<StockResponse> GetAvailableStockAsync(
        int warehouseId,
        int productId,
        CancellationToken ct)
    {
        await RequireAsync<WarehouseEntity>(
            warehouseId,
            "Warehouse",
            ct);

        var product = await RequireAsync<Product>(
            productId,
            "Product",
            ct);

        var stock = await _data.FindAsync<WarehouseStock>(
            ct,
            warehouseId,
            productId);

        return new StockResponse(
            warehouseId,
            productId,
            product.Name,
            product.SKU,
            stock?.Quantity ?? 0m);
    }

    public async Task<IReadOnlyList<StockResponse>> GetWarehouseStocksAsync(
        int warehouseId,
        CancellationToken ct)
    {
        await RequireAsync<WarehouseEntity>(
            warehouseId,
            "Warehouse",
            ct);

        return await _data.GetWarehouseStocksAsync(
            warehouseId,
            ct);
    }
}