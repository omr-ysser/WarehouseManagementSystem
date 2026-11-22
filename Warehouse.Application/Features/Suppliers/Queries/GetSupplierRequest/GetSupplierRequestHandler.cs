using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.GetSupplierRequest;

public sealed class GetSupplierRequestHandler(ISupplierRequestService service)
    : IRequestHandler<GetSupplierRequestQuery, SupplierRequestResponse>
{
    public Task<SupplierRequestResponse> Handle(
        GetSupplierRequestQuery query,
        CancellationToken ct) =>
        service.GetAsync(query.SupplierId, query.RequestId, ct);
}
