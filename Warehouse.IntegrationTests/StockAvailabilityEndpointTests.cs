using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Warehouse.IntegrationTests;

public class StockAvailabilityEndpointTests
    : IClassFixture<WarehouseApiFactory>, IDisposable
{
    private readonly HttpClient _client;

    public StockAvailabilityEndpointTests(
        WarehouseApiFactory factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(15, false)]
    public async Task Get_WithPositiveQuantity_ReturnsExpectedAvailability(
        int quantity,
        bool expected)
    {
        // Act
        var response = await _client.GetAsync(
            $"/api/stock-availability/1?quantity={quantity}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actual = await response.Content
            .ReadFromJsonAsync<bool>();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Get_WithInvalidQuantity_ReturnsBadRequest(
        int quantity)
    {
        var response = await _client.GetAsync(
            $"/api/stock-availability/1?quantity={quantity}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
