using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Infrastructure.Identity;

public class Permission
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string NormalizedKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; }
        = new List<RolePermission>();
}
