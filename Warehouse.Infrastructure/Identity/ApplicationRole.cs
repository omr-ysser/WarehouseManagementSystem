using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;

namespace Warehouse.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public ICollection<RolePermission> RolePermissions { get; set; }
        = new List<RolePermission>();
}
