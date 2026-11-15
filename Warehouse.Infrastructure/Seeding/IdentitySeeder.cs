using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Security;
using Warehouse.Infrastructure.Identity;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Seeding;

public sealed class IdentitySeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;

    public IdentitySeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }

    public async Task SeedAsync(
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "A development seed password is required.");
        }

        // Enable temporarily only when an admin password reset is needed.
        // Uses the password supplied through development configuration.
        const bool resetAdminPassword = false;

        const string adminEmail = "admin@example.com";

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        // 1. Create missing permission definitions.
        await SeedPermissionsAsync(cancellationToken);

        // 2. Create missing roles and development accounts.
        var accounts = new[]
        {
            (
                Role: "Admin",
                Email: adminEmail,
                Phone: "+201000000001"
            ),
            (
                Role: "Supervisor",
                Email: "supervisor@example.com",
                Phone: "+201000000002"
            ),
            (
                Role: "Manager",
                Email: "manager@example.com",
                Phone: "+201000000005"
            ),
            (
                Role: "Vendor",
                Email: "vendor@example.com",
                Phone: "+201000000003"
            ),
            (
                Role: "Customer",
                Email: "customer@example.com",
                Phone: "+201000000004"
            )
        };

        foreach (var account in accounts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var role = await _roles.FindByNameAsync(account.Role);

            var isNewRole = role is null;

            if (role is null)
            {
                role = new ApplicationRole
                {
                    Name = account.Role
                };

                Check(await _roles.CreateAsync(role));
            }

            // Preserve the original defaults for newly created roles.
            if (isNewRole)
            {
                await GrantMissingPermissionsAsync(
                    role.Id,
                    GetInitialPermissionKeys(account.Role),
                    cancellationToken);
            }

            // Grant module permissions to existing roles as well.
            if (account.Role is "Admin" or "Supervisor" or "Manager")
            {
                await GrantMissingPermissionsAsync(
                    role.Id,
                    PermissionCatalog.RiderModule,
                    cancellationToken);
            }

            var user = await _users.FindByEmailAsync(account.Email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    FullName = $"{account.Role} Demo",
                    Email = account.Email,
                    UserName = account.Email,
                    PhoneNumber = account.Phone,
                    LockoutEnabled = true
                };

                Check(await _users.CreateAsync(user, password));
            }

            // Repair missing role membership for existing demo accounts.
            if (!await _users.IsInRoleAsync(user, account.Role))
            {
                Check(await _users.AddToRoleAsync(
                    user,
                    account.Role));
            }
        }

        // 3. Optional development admin password reset.
        if (resetAdminPassword)
        {
            var admin = await _users.FindByEmailAsync(adminEmail)
                ?? throw new InvalidOperationException(
                    "Admin account was not found.");

            var resetToken =
                await _users.GeneratePasswordResetTokenAsync(admin);

            Check(await _users.ResetPasswordAsync(
                admin,
                resetToken,
                password));

            Check(await _users.SetLockoutEndDateAsync(
                admin,
                null));

            Check(await _users.ResetAccessFailedCountAsync(admin));
        }

        // 4. Commit all changes together.
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(
        CancellationToken cancellationToken)
    {
        // Include RiderModule explicitly so its definitions always exist.
        var keys = PermissionCatalog.All
            .Concat(PermissionCatalog.RiderModule)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existingNormalizedKeys = await _db.Permissions
            .Select(x => x.NormalizedKey)
            .ToListAsync(cancellationToken);

        var existing = existingNormalizedKeys.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedKey = key.ToUpperInvariant();

            if (!existing.Add(normalizedKey))
            {
                continue;
            }

            _db.Permissions.Add(new Permission
            {
                Key = key,
                NormalizedKey = normalizedKey,
                DisplayName = key
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task GrantMissingPermissionsAsync(
        string roleId,
        IEnumerable<string> keys,
        CancellationToken cancellationToken)
    {
        var normalizedKeys = keys
            .Select(x => x.ToUpperInvariant())
            .Distinct()
            .ToArray();

        var permissions = await _db.Permissions
            .Where(x => normalizedKeys.Contains(x.NormalizedKey))
            .ToListAsync(cancellationToken);

        if (permissions.Count != normalizedKeys.Length)
        {
            throw new InvalidOperationException(
                "One or more permission definitions are missing.");
        }

        var assignedIds = await _db.RolePermissions
            .Where(x => x.RoleId == roleId)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);

        var assigned = assignedIds.ToHashSet();

        foreach (var permission in permissions)
        {
            if (!assigned.Add(permission.Id))
            {
                continue;
            }

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string[] GetInitialPermissionKeys(string roleName)
    {
        return roleName switch
        {
            "Admin" => PermissionCatalog.All
                .Concat(PermissionCatalog.RiderModule)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),

            "Supervisor" =>
            [
                "Warehouses.View",
                "Products.View",
                "Transactions.View",
                "Transactions.Create",
                "Stock.View",

                .. PermissionCatalog.RiderModule
            ],

            "Manager" => PermissionCatalog.RiderModule,

            "Vendor" =>
            [
                "Products.View",
                "Stock.View"
            ],

            "Customer" =>
            [
                "Products.View"
            ],

            _ => []
        };
    }

    private static void Check(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException(
            string.Join(
                " ",
                result.Errors.Select(x => x.Description)));
    }
}