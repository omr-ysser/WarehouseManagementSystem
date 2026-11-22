using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed record SupplierRequestResponse(
    int Id,
    int SupplierId,
    string OrderId,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal LoadWeight,
    string FulfillmentSource,
    string Status,
    decimal? CollectedAmount,
    decimal? Payments,
    decimal? TotalAmount);
