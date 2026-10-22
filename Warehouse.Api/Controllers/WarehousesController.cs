using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public WarehousesController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:Warehouses.View")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await _inventory.GetWarehousesAsync(ct));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:Warehouses.View")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        return Ok(await _inventory.GetWarehouseAsync(id, ct));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Warehouses.Create")]
    public async Task<IActionResult> Create(
        WarehouseRequest request,
        CancellationToken ct)
    {
        var result = await _inventory.CreateWarehouseAsync(request, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:Warehouses.Edit")]
    public async Task<IActionResult> Update(
        int id,
        WarehouseRequest request,
        CancellationToken ct)
    {
        await _inventory.UpdateWarehouseAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Warehouses.Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _inventory.DeleteWarehouseAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/stock")]
    [Authorize(Policy = "Permission:Stock.View")]
    public async Task<IActionResult> GetStocks(int id, CancellationToken ct)
    {
        return Ok(await _inventory.GetWarehouseStocksAsync(id, ct));
    }

    [HttpGet("{warehouseId:int}/stock/{productId:int}")]
    [Authorize(Policy = "Permission:Stock.View")]
    public async Task<IActionResult> GetAvailableStock(
        int warehouseId,
        int productId,
        CancellationToken ct)
    {
        return Ok(await _inventory.GetAvailableStockAsync(
            warehouseId, productId, ct));
    }

    [HttpGet("{id:int}/transactions")]
    [Authorize(Policy = "Permission:Transactions.View")]
    public async Task<IActionResult> GetTransactions(
        int id,
        CancellationToken ct)
    {
        return Ok(await _inventory.GetTransactionsAsync(id, null, ct));
    }
}