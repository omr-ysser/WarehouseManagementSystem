using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Persistence.Repositories;

public sealed class InventoryData : IInventoryData
{
    private readonly ApplicationDbContext _db;

    public InventoryData(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<T?> FindAsync<T>(
        CancellationToken cancellationToken,
        params object[] keys)
        where T : class
    {
        return await _db.Set<T>()
            .FindAsync(keys, cancellationToken);
    }

    public void Add<T>(T entity) where T : class
    {
        _db.Set<T>().Add(entity);
    }

    public void Remove<T>(T entity) where T : class
    {
        _db.Set<T>().Remove(entity);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IInventoryTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        var transaction = await _db.Database
            .BeginTransactionAsync(cancellationToken);

        return new InventoryTransaction(transaction);
    }

    public Task<bool> WarehouseNameExistsAsync(
        string normalizedName,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        return _db.Warehouses.AnyAsync(
            x => x.NormalizedName == normalizedName
                 && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> ProductSkuExistsAsync(
        string normalizedSku,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        return _db.Products.AnyAsync(
            x => x.NormalizedSku == normalizedSku
                 && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<bool> WarehouseHasDependenciesAsync(
        int warehouseId,
        CancellationToken cancellationToken)
    {
        return await _db.StockTransactions.AnyAsync(
                   x => x.WarehouseId == warehouseId,
                   cancellationToken)
               || await _db.WarehouseStocks.AnyAsync(
                   x => x.WarehouseId == warehouseId,
                   cancellationToken);
    }

    public async Task<bool> ProductHasDependenciesAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await _db.StockTransactions.AnyAsync(
                   x => x.ProductId == productId,
                   cancellationToken)
               || await _db.WarehouseStocks.AnyAsync(
                   x => x.ProductId == productId,
                   cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseResponse>> GetWarehousesAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Warehouses
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new WarehouseResponse(x.Id, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ProductResponse(
                x.Id,
                x.Name,
                x.SKU,
                x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockTransactionResponse>>
        GetTransactionsAsync(
            int? warehouseId,
            int? productId,
            CancellationToken cancellationToken)
    {
        var query = _db.StockTransactions.AsNoTracking();

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        var transactions = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return transactions.Select(x => new StockTransactionResponse(
            x.Id,
            x.WarehouseId,
            x.ProductId,
            x.Quantity,
            x.CreatedAtUtc,
            x.CreatedByUserId,
            Convert.ToBase64String(x.RowVersion)))
            .ToArray();
    }

    public async Task<IReadOnlyList<StockResponse>> GetWarehouseStocksAsync(
        int warehouseId,
        CancellationToken cancellationToken)
    {
        return await _db.WarehouseStocks
            .AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId)
            .OrderBy(x => x.Product.Name)
            .Select(x => new StockResponse(
                x.WarehouseId,
                x.ProductId,
                x.Product.Name,
                x.Product.SKU,
                x.Quantity))
            .ToListAsync(cancellationToken);
    }

    private sealed class InventoryTransaction : IInventoryTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public InventoryTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            return _transaction.CommitAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}
