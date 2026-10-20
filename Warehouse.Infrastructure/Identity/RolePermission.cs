using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Infrastructure.Identity;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;

    public int PermissionId { get; set; }

    public ApplicationRole Role { get; set; } = null!;

    public Permission Permission { get; set; } = null!;
}
