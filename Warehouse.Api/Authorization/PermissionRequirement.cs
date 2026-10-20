using Microsoft.AspNetCore.Authorization;

namespace Warehouse.Api.Authorization;

public sealed record PermissionRequirement(string Permission)
    : IAuthorizationRequirement;
