using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;

namespace Warehouse.Application.Abstractions;

public interface IInventoryService
{
    Task<IReadOnlyList<WarehouseResponse>> GetWarehousesAsync(
        CancellationToken ct);

    Task<WarehouseResponse> GetWarehouseAsync(int id, CancellationToken ct);

    Task<WarehouseResponse> CreateWarehouseAsync(
        WarehouseRequest request, CancellationToken ct);

    Task UpdateWarehouseAsync(
        int id, WarehouseRequest request, CancellationToken ct);

    Task DeleteWarehouseAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<ProductResponse>> GetProductsAsync(
        CancellationToken ct);

    Task<ProductResponse> GetProductAsync(int id, CancellationToken ct);

    Task<ProductResponse> CreateProductAsync(
        ProductRequest request, CancellationToken ct);

    Task UpdateProductAsync(
        int id, ProductRequest request, CancellationToken ct);

    Task DeleteProductAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<StockTransactionResponse>> GetTransactionsAsync(
        int? warehouseId, int? productId, CancellationToken ct);

    Task<StockTransactionResponse> GetTransactionAsync(
        int id, CancellationToken ct);

    Task<StockTransactionResponse> CreateTransactionAsync(
        CreateStockTransactionRequest request,
        string userId,
        CancellationToken ct);

    Task<StockTransactionResponse> UpdateTransactionAsync(
        int id,
        UpdateStockTransactionRequest request,
        CancellationToken ct);

    Task DeleteTransactionAsync(
        int id,
        DeleteStockTransactionRequest request,
        CancellationToken ct);

    Task<StockResponse> GetAvailableStockAsync(
        int warehouseId, int productId, CancellationToken ct);

    Task<IReadOnlyList<StockResponse>> GetWarehouseStocksAsync(
        int warehouseId, CancellationToken ct);
}
