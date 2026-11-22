using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.ListSupplierFinanceActivity;

public sealed class ListSupplierFinanceActivityHandler(ISupplierRequestService service)
    : IRequestHandler<
        ListSupplierFinanceActivityQuery,
        IReadOnlyList<SupplierFinanceActivityResponse>>
{
    public Task<IReadOnlyList<SupplierFinanceActivityResponse>> Handle(
        ListSupplierFinanceActivityQuery query,
        CancellationToken ct) =>
        service.ListActivityAsync(query.SupplierId, query.RequestId, ct);
}
