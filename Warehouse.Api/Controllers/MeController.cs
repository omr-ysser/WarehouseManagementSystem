using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;

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
        var userId = User.FindFirst("sub")!.Value;

        return Ok(await _identity.GetCurrentUserAsync(
            userId, cancellationToken));
    }
}