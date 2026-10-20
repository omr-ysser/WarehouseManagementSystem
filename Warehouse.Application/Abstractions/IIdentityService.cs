using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;

namespace Warehouse.Application.Abstractions;

public interface IIdentityService
{
    Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<bool> IsUserAllowedAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<bool> HasPermissionAsync(
        string userId,
        string permission,
        CancellationToken cancellationToken);

    Task<CurrentUserResponse> GetCurrentUserAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleResponse>> GetRolesAsync(
        CancellationToken cancellationToken);

    Task<RoleResponse> CreateRoleAsync(
        RoleRequest request,
        CancellationToken cancellationToken);

    Task UpdateRoleAsync(
        string id,
        RoleRequest request,
        CancellationToken cancellationToken);

    Task SetRolePermissionsAsync(
        string roleId,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionResponse>> GetPermissionsAsync(
        CancellationToken cancellationToken);

    Task<PermissionResponse> CreatePermissionAsync(
        CreatePermissionRequest request,
        CancellationToken cancellationToken);

    Task UpdatePermissionAsync(
        int id,
        UpdatePermissionRequest request,
        CancellationToken cancellationToken);

    Task DeletePermissionAsync(
        int id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserResponse>> GetUsersAsync(
        string? roleId,
        CancellationToken cancellationToken);

    Task<UserResponse> GetUserAsync(
        string id,
        CancellationToken cancellationToken);

    Task<UserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken);

    Task UpdateUserAsync(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken);

    Task AssignRoleAsync(
        string userId,
        string roleId,
        CancellationToken cancellationToken);
}
