using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warehouse.Domain.Entities;

namespace Warehouse.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string NormalizedSku { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<WarehouseStock> Stocks { get; set; }
        = new List<WarehouseStock>();

    public ICollection<StockTransaction> Transactions { get; set; }
        = new List<StockTransaction>();
}
