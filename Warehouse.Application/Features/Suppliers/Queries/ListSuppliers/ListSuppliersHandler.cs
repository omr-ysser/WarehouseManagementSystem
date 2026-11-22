using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.ListSuppliers;

public sealed class ListSuppliersHandler(ISupplierService service)
    : IRequestHandler<ListSuppliersQuery, SupplierListResponse>
{
    public Task<SupplierListResponse> Handle(
        ListSuppliersQuery query,
        CancellationToken ct)
    {
        return service.ListAsync(query.Parameters, ct);
    }
}