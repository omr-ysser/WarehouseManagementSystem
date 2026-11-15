using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Features.Products.CreateProduct;
using Warehouse.Application.Features.Products.DeleteProduct;
using Warehouse.Application.Features.Products.GetProductById;
using Warehouse.Application.Features.Products.GetProducts;
using Warehouse.Application.Features.Products.GetProductTransactions;
using Warehouse.Application.Features.Products.UpdateProduct;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:Products.View")]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetProductsQuery(),
            ct);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:Products.View")]
    public async Task<ActionResult<ProductResponse>> GetById(
        int id,
        CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetProductByIdQuery(id),
            ct);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Products.Create")]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] ProductRequest request,
        CancellationToken ct)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.SKU,
            request.Description);

        var result = await _sender.Send(command, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:Products.Edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ProductRequest request,
        CancellationToken ct)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.SKU,
            request.Description);

        await _sender.Send(command, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Products.Delete")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        await _sender.Send(
            new DeleteProductCommand(id),
            ct);

        return NoContent();
    }

    [HttpGet("{id:int}/transactions")]
    [Authorize(Policy = "Permission:Transactions.View")]
    public async Task<ActionResult<IReadOnlyList<StockTransactionResponse>>>
        GetTransactions(
            int id,
            CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetProductTransactionsQuery(id),
            ct);

        return Ok(result);
    }
}