using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
public sealed class CustomerPasswordsController : ControllerBase
{
    private readonly ICustomerPasswordService _passwords;

    public CustomerPasswordsController(
        ICustomerPasswordService passwords)
    {
        _passwords = passwords;
    }

    [AllowAnonymous]
    [EnableRateLimiting("customer-password")]
    [HttpPost("api/auth/customer-password")]
    public async Task<IActionResult> SetPassword(
        [FromBody] SetCustomerPasswordRequest request,
        CancellationToken ct)
    {
        await _passwords.SetPasswordAsync(
            request,
            ct);

        return Ok(new
        {
            Message = "Password created successfully. You can now sign in."
        });
    }

    [Authorize(Policy = "Permission:Riders.Create")]
    [HttpPost("api/riders/{id:int}/password-invitation")]
    public async Task<IActionResult> SendInvitation(
        int id,
        CancellationToken ct)
    {
        await _passwords.QueueInvitationAsync(
            id,
            ct);

        return Accepted(new
        {
            Message = "Password invitation queued for delivery."
        });
    }
}
