using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.Application.DTOs;

// Authentication

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string TokenType = "Bearer");

public sealed record AuthenticatedUser(
    string Id,
    string FullName,
    string Email);

// Roles

public sealed class RoleRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public sealed record RoleResponse(
    string Id,
    string Name,
    IReadOnlyList<int> PermissionIds);

public sealed class SetRolePermissionsRequest
{
    [Required, MaxLength(500)]
    public int[] PermissionIds { get; set; } = Array.Empty<int>();
}

// Permissions

public sealed class CreatePermissionRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    [RegularExpression(
        @"^[A-Za-z][A-Za-z0-9]*\.[A-Za-z][A-Za-z0-9]*$",
        ErrorMessage = "Use a key such as Products.View.")]
    public string Key { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class UpdatePermissionRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed record PermissionResponse(
    int Id,
    string Key,
    string DisplayName);

// Users

public sealed class CreateUserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\+[1-9]\d{7,14}$")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(450)]
    public string RoleId { get; set; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\+[1-9]\d{7,14}$")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public sealed class AssignRoleRequest
{
    [Required, MaxLength(450)]
    public string RoleId { get; set; } = string.Empty;
}

public sealed record UserResponse(
    string Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string? RoleId,
    string? RoleName);

// Navigation

public sealed record NavigationItemResponse(
    string Key,
    string Label,
    string Path);

// Current user

public sealed record CurrentUserResponse(
    UserResponse User,
    IReadOnlyList<string> Permissions)
{
    public IReadOnlyList<NavigationItemResponse> Navigation { get; init; }
        = Array.Empty<NavigationItemResponse>();
}