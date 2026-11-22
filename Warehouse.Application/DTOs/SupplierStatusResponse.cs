using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed record SupplierStatusResponse(int SupplierId, bool IsActive)
{
    public string Status => IsActive ? "Active" : "Inactive";
}