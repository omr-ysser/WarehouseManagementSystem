using Warehouse.Application.Abstractions;
using Warehouse.Application.Services;
using Xunit;

namespace Warehouse.UnitTests;

public class StockAvailabilityServiceTests
{
    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(10, 10, true)]
    [InlineData(10, 15, false)]
    [InlineData(10, 0, false)]
    [InlineData(10, -2, false)]
    public async Task CanWithdrawAsync_ReturnsExpectedResult(
        int availableStock,
        int requestedQuantity,
        bool expected)
    {
        // Arrange
        var stockReader = new FakeStockReader(availableStock);
        var service = new StockAvailabilityService(stockReader);

        // Act
        var actual = await service.CanWithdrawAsync(
            productId: 1,
            quantity: requestedQuantity);

        // Assert
        Assert.Equal(expected, actual);
    }

    private sealed class FakeStockReader : IStockReader
    {
        private readonly int _availableStock;

        public FakeStockReader(int availableStock)
        {
            _availableStock = availableStock;
        }

        public Task<int> GetAvailableStockAsync(int productId)
        {
            return Task.FromResult(_availableStock);
        }
    }
}
