using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IIdentityService _identity;

    public RolesController(IIdentityService identity)
    {
        _identity = identity;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:Roles.View")]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _identity.GetRolesAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Roles.Create")]
    public async Task<IActionResult> Create(
        RoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identity.CreateRoleAsync(
            request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Permission:Roles.Edit")]
    public async Task<IActionResult> Update(
        string id,
        RoleRequest request,
        CancellationToken cancellationToken)
    {
        await _identity.UpdateRoleAsync(
            id, request, cancellationToken);

        return NoContent();
    }

    [HttpPut("{id}/permissions")]
    [Authorize(Policy = "Permission:Roles.EditPermissions")]
    public async Task<IActionResult> SetPermissions(
        string id,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        await _identity.SetRolePermissionsAsync(
            id, request, cancellationToken);

        return NoContent();
    }

    [HttpGet("{id}/users")]
    [Authorize(Policy = "Permission:Users.View")]
    public async Task<IActionResult> GetUsers(
        string id,
        CancellationToken cancellationToken)
    {
        return Ok(await _identity.GetUsersAsync(
            id, cancellationToken));
    }
}