using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.GetSupplierById;

public sealed class GetSupplierByIdHandler
    : IRequestHandler<GetSupplierByIdQuery, SupplierDetailsResponse>
{
    private readonly ISupplierService _supplierService;

    public GetSupplierByIdHandler(
        ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public Task<SupplierDetailsResponse> Handle(
        GetSupplierByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _supplierService.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
