using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;

namespace Warehouse.Application.Abstractions;

public interface ISupplierRequestService
{
    Task<IReadOnlyList<SupplierRequestResponse>> ListAsync(
        int supplierId,
        SupplierRequestFilter filter,
        CancellationToken ct);

    Task<SupplierRequestResponse> GetAsync(
        int supplierId,
        int requestId,
        CancellationToken ct);

    Task<SupplierRequestResponse> CreateAsync(
        int supplierId,
        CreateSupplierRequestInput input,
        CancellationToken ct);

    Task<SupplierRequestResponse> UpdateFinanceAsync(
        int supplierId,
        int requestId,
        UpdateSupplierFinanceInput input,
        string actorAccountId,
        CancellationToken ct);

    Task<IReadOnlyList<SupplierFinanceActivityResponse>> ListActivityAsync(
        int supplierId,
        int requestId,
        CancellationToken ct);
}