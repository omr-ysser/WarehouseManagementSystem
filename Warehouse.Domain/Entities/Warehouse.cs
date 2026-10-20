using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public class Warehouse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<WarehouseStock> Stocks { get; set; }
        = new List<WarehouseStock>();

    public ICollection<StockTransaction> Transactions { get; set; }
        = new List<StockTransaction>();
}
