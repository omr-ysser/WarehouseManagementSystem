using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.ExportSuppliers;

public sealed class ExportSuppliersHandler(ISupplierService service)
    : IRequestHandler<ExportSuppliersQuery, IReadOnlyList<SupplierListItemResponse>>
{
    public Task<IReadOnlyList<SupplierListItemResponse>> Handle(
        ExportSuppliersQuery query,
        CancellationToken ct)
    {
        return service.ExportAsync(query.Filter, ct);
    }
}
