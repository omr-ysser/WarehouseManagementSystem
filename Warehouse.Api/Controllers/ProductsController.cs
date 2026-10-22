using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public ProductsController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:Products.View")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await _inventory.GetProductsAsync(ct));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:Products.View")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        return Ok(await _inventory.GetProductAsync(id, ct));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Products.Create")]
    public async Task<IActionResult> Create(
        ProductRequest request,
        CancellationToken ct)
    {
        var result = await _inventory.CreateProductAsync(request, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:Products.Edit")]
    public async Task<IActionResult> Update(
        int id,
        ProductRequest request,
        CancellationToken ct)
    {
        await _inventory.UpdateProductAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Products.Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _inventory.DeleteProductAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/transactions")]
    [Authorize(Policy = "Permission:Transactions.View")]
    public async Task<IActionResult> GetTransactions(
        int id,
        CancellationToken ct)
    {
        return Ok(await _inventory.GetTransactionsAsync(null, id, ct));
    }
}
