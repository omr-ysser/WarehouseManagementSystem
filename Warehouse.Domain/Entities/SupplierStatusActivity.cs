using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public sealed class SupplierStatusActivity
{
    public long Id { get; set; }
    public int SupplierId { get; set; }

    public bool PreviousIsActive { get; set; }
    public bool NewIsActive { get; set; }

    public string ActorAccountId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
