using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.Abstractions;

namespace Warehouse.Application.Services;

public class StockAvailabilityService
{
    private readonly IStockReader _stockReader;

    public StockAvailabilityService(IStockReader stockReader)
    {
        _stockReader = stockReader;
    }

    public async Task<bool> CanWithdrawAsync(
        int productId,
        int quantity)
    {
        if (quantity <= 0)
            return false;

        var available =
            await _stockReader.GetAvailableStockAsync(productId);

        return available >= quantity;
    }
}
