using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public sealed class SupplierRequestFinanceActivity
{
    public long Id { get; set; }
    public int SupplierRequestId { get; set; }

    public string ChangedByAccountId { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }

    public decimal? PreviousCollectedAmount { get; set; }
    public decimal? PreviousPayments { get; set; }

    public decimal CollectedAmount { get; set; }
    public decimal Payments { get; set; }
}
