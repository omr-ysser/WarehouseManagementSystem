using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IIdentityService _identity;

    public UsersController(IIdentityService identity)
    {
        _identity = identity;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:Users.View")]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _identity.GetUsersAsync(
            null, cancellationToken));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Permission:Users.View")]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        return Ok(await _identity.GetUserAsync(
            id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Users.Create")]
    [Authorize(Policy = "Permission:Users.AssignRole")]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identity.CreateUserAsync(
            request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Permission:Users.Edit")]
    public async Task<IActionResult> Update(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        await _identity.UpdateUserAsync(
            id, request, cancellationToken);

        return NoContent();
    }

    [HttpPut("{id}/role")]
    [Authorize(Policy = "Permission:Users.AssignRole")]
    public async Task<IActionResult> AssignRole(
        string id,
        AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _identity.AssignRoleAsync(
            id, request.RoleId, cancellationToken);

        return NoContent();
    }
}