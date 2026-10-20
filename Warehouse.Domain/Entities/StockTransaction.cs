using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public class StockTransaction
{
    public int Id { get; set; }

    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public decimal Quantity { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Warehouse Warehouse { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
