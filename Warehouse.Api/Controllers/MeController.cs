using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Security;

namespace Warehouse.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private readonly IIdentityService _identity;

    public MeController(IIdentityService identity)
    {
        _identity = identity;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var currentUser = await _identity.GetCurrentUserAsync(
            userId,
            cancellationToken);

        return Ok(currentUser with
        {
            Navigation = NavigationCatalog.GetItems(
                currentUser.Permissions)
        });
    }
}