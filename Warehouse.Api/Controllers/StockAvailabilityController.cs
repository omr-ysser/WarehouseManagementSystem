using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Services;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/stock-availability")]
public class StockAvailabilityController : ControllerBase
{
    private readonly StockAvailabilityService _service;

    public StockAvailabilityController(
        StockAvailabilityService service)
    {
        _service = service;
    }

    [HttpGet("{productId:int}")]
    public async Task<ActionResult<bool>> Get(
        int productId,
        [FromQuery] int quantity)
    {
        if (quantity <= 0)
            return BadRequest("Quantity must be positive.");

        var result = await _service.CanWithdrawAsync(
            productId,
            quantity);

        return Ok(result);
    }
}
