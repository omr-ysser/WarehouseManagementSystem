using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed record SupplierFinanceActivityResponse(
    string ChangedByAccountId,
    DateTime ChangedAtUtc,
    decimal? PreviousCollectedAmount,
    decimal? PreviousPayments,
    decimal CollectedAmount,
    decimal Payments);
