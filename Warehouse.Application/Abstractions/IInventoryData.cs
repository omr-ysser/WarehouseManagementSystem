using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;

namespace Warehouse.Application.Abstractions;

public interface IInventoryTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}

public interface IInventoryData
{
    Task<T?> FindAsync<T>(
        CancellationToken cancellationToken,
        params object[] keys)
        where T : class;

    void Add<T>(T entity) where T : class;

    void Remove<T>(T entity) where T : class;

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<IInventoryTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken);

    Task<bool> WarehouseNameExistsAsync(
        string normalizedName,
        int? excludingId,
        CancellationToken cancellationToken);

    Task<bool> ProductSkuExistsAsync(
        string normalizedSku,
        int? excludingId,
        CancellationToken cancellationToken);

    Task<bool> WarehouseHasDependenciesAsync(
        int warehouseId,
        CancellationToken cancellationToken);

    Task<bool> ProductHasDependenciesAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WarehouseResponse>> GetWarehousesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductResponse>> GetProductsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StockTransactionResponse>> GetTransactionsAsync(
        int? warehouseId,
        int? productId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StockResponse>> GetWarehouseStocksAsync(
        int warehouseId,
        CancellationToken cancellationToken);
}
