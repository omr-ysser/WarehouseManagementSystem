using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.Security;

public static class PermissionCatalog
{
    public static readonly string[] All =
    [
        "Users.View",
        "Users.Create",
        "Users.Edit",
        "Users.AssignRole",

        "Roles.View",
        "Roles.Create",
        "Roles.Edit",
        "Roles.EditPermissions",

        "Permissions.View",
        "Permissions.Create",
        "Permissions.Edit",
        "Permissions.Delete",

        "Warehouses.View",
        "Warehouses.Create",
        "Warehouses.Edit",
        "Warehouses.Delete",

        "Products.View",
        "Products.Create",
        "Products.Edit",
        "Products.Delete",

        "Transactions.View",
        "Transactions.Create",
        "Transactions.Edit",
        "Transactions.Delete",

        "Stock.View"
    ];

    public static bool IsBuiltIn(string key)
    {
        return All.Contains(key, StringComparer.OrdinalIgnoreCase);
    }
}
