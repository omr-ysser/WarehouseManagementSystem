using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed class SupplierListParameters : SupplierListFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
