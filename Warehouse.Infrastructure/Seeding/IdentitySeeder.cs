using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(" ", result.Errors.Select(x => x.Description)));
        }
    }

    public async Task SeedAsync(
        string password,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var key in PermissionCatalog.All)
        {
            var normalized = key.ToUpperInvariant();

            if (!await _db.Permissions.AnyAsync(
                x => x.NormalizedKey == normalized,
                cancellationToken))
            {
                _db.Permissions.Add(new Permission
                {
                    Key = key,
                    NormalizedKey = normalized,
                    DisplayName = key
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var accounts = new[]
        {
            ("Admin", "admin@example.com", "+201000000001"),
            ("Supervisor", "supervisor@example.com", "+201000000002"),
            ("Vendor", "vendor@example.com", "+201000000003"),
            ("Customer", "customer@example.com", "+201000000004")
        };

        foreach (var (roleName, email, phone) in accounts)
        {
            var role = await _roles.FindByNameAsync(roleName);

            // Assign initial permissions only when creating the role.
            // Existing role assignments are never reset by startup.
            if (role is null)
            {
                role = new ApplicationRole { Name = roleName };
                Check(await _roles.CreateAsync(role));

                var keys = roleName switch
                {
                    "Admin" => PermissionCatalog.All,

                    "Supervisor" => new[]
                    {
                        "Warehouses.View",
                        "Products.View",
                        "Transactions.View",
                        "Transactions.Create",
                        "Stock.View"
                    },

                    "Vendor" => new[]
                    {
                        "Products.View",
                        "Stock.View"
                    },

                    _ => new[] { "Products.View" }
                };

                var normalizedKeys = keys
                    .Select(x => x.ToUpperInvariant())
                    .ToArray();

                var permissions = await _db.Permissions
                    .Where(x => normalizedKeys.Contains(x.NormalizedKey))
                    .ToListAsync(cancellationToken);

                foreach (var permission in permissions)
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                }

                await _db.SaveChangesAsync(cancellationToken);
            }

            if (await _users.FindByEmailAsync(email) is null)
            {
                var user = new ApplicationUser
                {
                    FullName = $"{roleName} Demo",
                    Email = email,
                    UserName = email,
                    PhoneNumber = phone,
                    LockoutEnabled = true
                };

                Check(await _users.CreateAsync(user, password));
                Check(await _users.AddToRoleAsync(user, roleName));
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
