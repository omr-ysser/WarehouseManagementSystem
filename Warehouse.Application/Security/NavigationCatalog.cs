using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;

namespace Warehouse.Application.Security;

public static class NavigationCatalog
{
    public static IReadOnlyList<NavigationItemResponse> GetItems(
        IEnumerable<string> permissions)
    {
        var allowed = permissions.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        var definitions = new[]
        {
            ("users", "Users", "/users", "Users.View"),
            ("roles", "Roles", "/roles", "Roles.View"),
            ("permissions", "Permissions", "/permissions", "Permissions.View"),
            ("warehouses", "Warehouses", "/warehouses", "Warehouses.View"),
            ("products", "Products", "/products", "Products.View"),
            ("transactions", "Transactions", "/transactions", "Transactions.View"),
            ("stock", "Stock", "/stock", "Stock.View")
        };

        return definitions
            .Where(x => allowed.Contains(x.Item4))
            .Select(x => new NavigationItemResponse(
                x.Item1,
                x.Item2,
                x.Item3))
            .ToArray();
    }
}
