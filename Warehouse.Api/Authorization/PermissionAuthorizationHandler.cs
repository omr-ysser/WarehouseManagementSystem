using Microsoft.AspNetCore.Authorization;
using Warehouse.Application.Abstractions;

namespace Warehouse.Api.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IIdentityService _identity;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(
        IIdentityService identity,
        IHttpContextAccessor httpContextAccessor)
    {
        _identity = identity;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var cancellationToken =
            _httpContextAccessor.HttpContext?.RequestAborted
            ?? CancellationToken.None;

        if (await _identity.HasPermissionAsync(
            userId,
            requirement.Permission,
            cancellationToken))
        {
            context.Succeed(requirement);
        }
    }
}