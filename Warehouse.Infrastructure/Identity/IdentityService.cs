using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Application.Security;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;

    public IdentityService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }

    private static string Clean(
        string value,
        int minimum,
        int maximum,
        string field)
    {
        var cleaned = value.Trim();

        if (cleaned.Length < minimum || cleaned.Length > maximum)
        {
            throw new AppException(
                400,
                $"{field} must contain between {minimum} and {maximum} characters.");
        }

        return cleaned;
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors.ToArray();

        var status = errors.Any(x =>
            x.Code.StartsWith("Duplicate", StringComparison.Ordinal) ||
            x.Code == "ConcurrencyFailure")
            ? 409
            : 400;

        throw new AppException(
            status,
            string.Join(" ", errors.Select(x => x.Description)));
    }

    private async Task<ApplicationUser> RequireUserAsync(string id)
    {
        return await _users.FindByIdAsync(id)
            ?? throw new AppException(404, "User not found.");
    }

    private async Task<ApplicationRole> RequireRoleAsync(string id)
    {
        return await _roles.FindByIdAsync(id)
            ?? throw new AppException(404, "Role not found.");
    }

    public async Task<bool> IsUserAllowedAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _users.FindByIdAsync(userId);

        return user is not null &&
               !await _users.IsLockedOutAsync(user);
    }

    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _users.FindByEmailAsync(email);

        if (user is null || await _users.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await _users.CheckPasswordAsync(user, password))
        {
            EnsureSuccess(await _users.AccessFailedAsync(user));
            return null;
        }

        EnsureSuccess(await _users.ResetAccessFailedCountAsync(user));

        return new AuthenticatedUser(
            user.Id,
            user.FullName,
            user.Email!);
    }

    public async Task<bool> HasPermissionAsync(
        string userId,
        string permission,
        CancellationToken cancellationToken)
    {
        var normalized = permission.ToUpperInvariant();

        return await (
            from userRole in _db.UserRoles
            join rolePermission in _db.RolePermissions
                on userRole.RoleId equals rolePermission.RoleId
            join definition in _db.Permissions
                on rolePermission.PermissionId equals definition.Id
            where userRole.UserId == userId
                  && definition.NormalizedKey == normalized
            select definition.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        var permissions = await (
            from userRole in _db.UserRoles
            join rolePermission in _db.RolePermissions
                on userRole.RoleId equals rolePermission.RoleId
            join definition in _db.Permissions
                on rolePermission.PermissionId equals definition.Id
            where userRole.UserId == userId
            select definition.Key
        )
        .Distinct()
        .OrderBy(x => x)
        .ToListAsync(cancellationToken);

        return new CurrentUserResponse(user, permissions);
    }

    // Roles

    public async Task<IReadOnlyList<RoleResponse>> GetRolesAsync(
        CancellationToken cancellationToken)
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(x => new RoleResponse(
            x.Id,
            x.Name!,
            x.RolePermissions
                .Select(p => p.PermissionId)
                .OrderBy(id => id)
                .ToArray()))
            .ToArray();
    }

    public async Task<RoleResponse> CreateRoleAsync(
        RoleRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = new ApplicationRole
        {
            Name = Clean(request.Name, 2, 50, "Role name")
        };

        EnsureSuccess(await _roles.CreateAsync(role));

        return new RoleResponse(
            role.Id,
            role.Name!,
            Array.Empty<int>());
    }

    public async Task UpdateRoleAsync(
        string id,
        RoleRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await RequireRoleAsync(id);

        role.Name = Clean(request.Name, 2, 50, "Role name");

        EnsureSuccess(await _roles.UpdateAsync(role));
    }

    public async Task SetRolePermissionsAsync(
        string roleId,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PermissionIds.Any(x => x <= 0))
        {
            throw new AppException(400, "Permission IDs must be positive.");
        }

        if (request.PermissionIds.Distinct().Count()
            != request.PermissionIds.Length)
        {
            throw new AppException(400, "Duplicate permission IDs.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        await RequireRoleAsync(roleId);

        var ids = request.PermissionIds;

        var existingCount = await _db.Permissions
            .CountAsync(x => ids.Contains(x.Id), cancellationToken);

        if (existingCount != ids.Length)
        {
            throw new AppException(
                400,
                "One or more permissions do not exist.");
        }

        var current = await _db.RolePermissions
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _db.RolePermissions.RemoveRange(
            current.Where(x => !ids.Contains(x.PermissionId)));

        var currentIds = current
            .Select(x => x.PermissionId)
            .ToHashSet();

        foreach (var id in ids.Where(x => !currentIds.Contains(x)))
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Permissions

    public async Task<IReadOnlyList<PermissionResponse>>
        GetPermissionsAsync(CancellationToken cancellationToken)
    {
        return await _db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new PermissionResponse(
                x.Id,
                x.Key,
                x.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<PermissionResponse> CreatePermissionAsync(
        CreatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var key = Clean(request.Key, 3, 100, "Permission key");
        var normalized = key.ToUpperInvariant();

        if (await _db.Permissions.AnyAsync(
            x => x.NormalizedKey == normalized,
            cancellationToken))
        {
            throw new AppException(409, "Permission already exists.");
        }

        var permission = new Permission
        {
            Key = key,
            NormalizedKey = normalized,
            DisplayName = Clean(
                request.DisplayName, 2, 100, "Display name")
        };

        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync(cancellationToken);

        return new PermissionResponse(
            permission.Id,
            permission.Key,
            permission.DisplayName);
    }

    public async Task UpdatePermissionAsync(
        int id,
        UpdatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var permission = await _db.Permissions
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "Permission not found.");

        permission.DisplayName = Clean(
            request.DisplayName, 2, 100, "Display name");

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePermissionAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var permission = await _db.Permissions
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "Permission not found.");

        if (PermissionCatalog.IsBuiltIn(permission.Key))
        {
            throw new AppException(
                409,
                "Built-in endpoint permissions cannot be deleted.");
        }

        if (await _db.RolePermissions.AnyAsync(
            x => x.PermissionId == id, cancellationToken))
        {
            throw new AppException(
                409,
                "Remove role assignments before deleting this permission.");
        }

        _db.Permissions.Remove(permission);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Users

    public async Task<IReadOnlyList<UserResponse>> GetUsersAsync(
        string? roleId,
        CancellationToken cancellationToken)
    {
        if (roleId is not null)
        {
            await RequireRoleAsync(roleId);
        }

        var query =
            from user in _db.Users.AsNoTracking()
            join userRole in _db.UserRoles
                on user.Id equals userRole.UserId into assignments
            from assignment in assignments.DefaultIfEmpty()
            join role in _db.Roles
                on assignment.RoleId equals role.Id into roles
            from role in roles.DefaultIfEmpty()
            select new UserResponse(
                user.Id,
                user.FullName,
                user.Email!,
                user.PhoneNumber!,
                role == null ? null : role.Id,
                role == null ? null : role.Name);

        if (roleId is not null)
        {
            query = query.Where(x => x.RoleId == roleId);
        }

        return await query
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserResponse> GetUserAsync(
        string id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await RequireUserAsync(id);

        var role = await (
            from assignment in _db.UserRoles
            join item in _db.Roles
                on assignment.RoleId equals item.Id
            where assignment.UserId == id
            select item
        ).AsNoTracking().SingleOrDefaultAsync(cancellationToken);

        return new UserResponse(
            user.Id,
            user.FullName,
            user.Email!,
            user.PhoneNumber!,
            role?.Id,
            role?.Name);
    }

    public async Task<UserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var role = await RequireRoleAsync(request.RoleId);

        var email = request.Email.Trim();

        var user = new ApplicationUser
        {
            FullName = Clean(request.FullName, 2, 100, "Full name"),
            Email = email,
            UserName = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            LockoutEnabled = true
        };

        EnsureSuccess(await _users.CreateAsync(user, request.Password));

        EnsureSuccess(await _users.AddToRoleAsync(user, role.Name!));

        await transaction.CommitAsync(cancellationToken);

        return new UserResponse(
            user.Id,
            user.FullName,
            user.Email!,
            user.PhoneNumber!,
            role.Id,
            role.Name);
    }

    public async Task UpdateUserAsync(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await RequireUserAsync(id);

        user.FullName = Clean(request.FullName, 2, 100, "Full name");
        user.Email = request.Email.Trim();
        user.UserName = user.Email;
        user.PhoneNumber = request.PhoneNumber.Trim();

        EnsureSuccess(await _users.UpdateAsync(user));
    }

    public async Task AssignRoleAsync(
        string userId,
        string roleId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var user = await RequireUserAsync(userId);
        var role = await RequireRoleAsync(roleId);

        var currentRoles = await _users.GetRolesAsync(user);

        if (currentRoles.Count == 1 &&
            string.Equals(
                currentRoles[0],
                role.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (currentRoles.Count > 0)
        {
            EnsureSuccess(
                await _users.RemoveFromRolesAsync(user, currentRoles));
        }

        EnsureSuccess(await _users.AddToRoleAsync(user, role.Name!));

        await transaction.CommitAsync(cancellationToken);
    }
}
