using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Domain.Entities;

namespace Warehouse.Application.Abstractions;

public interface ICustomerAccountService
{
    // Called within the transaction that creates the rider.
    Task<string> CreateAccountAsync(
        Rider rider,
        CancellationToken ct);

    Task SynchronizeNameAsync(
        Rider rider,
        CancellationToken ct);

    Task<bool> IsDisabledAsync(
        string accountId,
        CancellationToken ct);

    Task<int?> GetRiderIdAsync(
        string accountId,
        CancellationToken ct);

    // Explicitly provisions an account for a legacy rider.
    Task ProvisionExistingRiderAsync(
        int riderId,
        CancellationToken ct);
}
