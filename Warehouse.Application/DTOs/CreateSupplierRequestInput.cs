using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Domain.Enums;

namespace Warehouse.Application.DTOs;

public sealed record CreateSupplierRequestInput(
    string OrderId,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal LoadWeight,
    SupplierFulfillmentSource FulfillmentSource);