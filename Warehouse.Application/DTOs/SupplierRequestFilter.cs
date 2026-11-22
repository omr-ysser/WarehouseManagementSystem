using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Domain.Enums;

namespace Warehouse.Application.DTOs;

public sealed class SupplierRequestFilter
{
    public SupplierFulfillmentSource? FulfillmentSource { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
