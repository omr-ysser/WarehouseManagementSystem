using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IIdentityService _identity;

    public PermissionsController(IIdentityService identity)
    {
        _identity = identity;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:Permissions.View")]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _identity.GetPermissionsAsync(
            cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Permissions.Create")]
    public async Task<IActionResult> Create(
        CreatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identity.CreatePermissionAsync(
            request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:Permissions.Edit")]
    public async Task<IActionResult> Update(
        int id,
        UpdatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        await _identity.UpdatePermissionAsync(
            id, request, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Permissions.Delete")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _identity.DeletePermissionAsync(
            id, cancellationToken);

        return NoContent();
    }
}