using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
public sealed class CustomerAccountsController : ControllerBase
{
    private readonly ICustomerAccountService _accounts;
    private readonly IRiderService _riders;

    public CustomerAccountsController(
        ICustomerAccountService accounts,
        IRiderService riders)
    {
        _accounts = accounts;
        _riders = riders;
    }

    [Authorize]
    [HttpGet("api/me/rider")]
    public async Task<ActionResult<RiderResponse>> GetMyProfile(
        CancellationToken ct)
    {
        var accountId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var riderId = await _accounts.GetRiderIdAsync(
            accountId,
            ct);

        if (riderId is null)
        {
            return NotFound(new
            {
                Message = "No customer profile is linked to this account."
            });
        }

        var profile = await _riders.GetRiderAsync(
            riderId.Value,
            ct);

        return Ok(profile);
    }

    [Authorize(Policy = "Permission:Riders.Create")]
    [HttpPost("api/riders/{id:int}/account")]
    public async Task<IActionResult> ProvisionAccount(
        int id,
        CancellationToken ct)
    {
        await _accounts.ProvisionExistingRiderAsync(
            id,
            ct);

        return NoContent();
    }
}
