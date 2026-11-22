using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.ListSupplierRequests;

public sealed class ListSupplierRequestsHandler(ISupplierRequestService service)
    : IRequestHandler<ListSupplierRequestsQuery, IReadOnlyList<SupplierRequestResponse>>
{
    public Task<IReadOnlyList<SupplierRequestResponse>> Handle(
        ListSupplierRequestsQuery query,
        CancellationToken ct) =>
        service.ListAsync(query.SupplierId, query.Filter, ct);
}
