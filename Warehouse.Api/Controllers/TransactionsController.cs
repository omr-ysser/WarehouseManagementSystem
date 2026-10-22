using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public TransactionsController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:Transactions.View")]
    public async Task<IActionResult> GetAll(
        CancellationToken ct)
    {
        return Ok(await _inventory.GetTransactionsAsync(null, null, ct));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:Transactions.View")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        return Ok(await _inventory.GetTransactionAsync(id, ct));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Transactions.Create")]
    public async Task<IActionResult> Create(
        CreateStockTransactionRequest request,
        CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _inventory.CreateTransactionAsync(
            request, userId, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:Transactions.Edit")]
    public async Task<IActionResult> Update(
        int id,
        UpdateStockTransactionRequest request,
        CancellationToken ct)
    {
        return Ok(await _inventory.UpdateTransactionAsync(id, request, ct));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Transactions.Delete")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] DeleteStockTransactionRequest request,
        CancellationToken ct)
    {
        await _inventory.DeleteTransactionAsync(id, request, ct);
        return NoContent();
    }
}