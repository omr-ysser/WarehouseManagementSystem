using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.Abstractions;

public interface IStockReader
{
    Task<int> GetAvailableStockAsync(int productId);
}
