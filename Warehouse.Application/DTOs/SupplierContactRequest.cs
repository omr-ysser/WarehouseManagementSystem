using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed class SupplierContactRequest
{
    public string Person { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;
}
