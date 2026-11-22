using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed record SupplierListItemResponse(
    int Id,
    string Name,
    string? Email,
    int TruckCount,
    bool IsActive)
{
    public string Status => IsActive ? "Active" : "Inactive";
}