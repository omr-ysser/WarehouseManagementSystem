using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.GetSupplierDocumentFile;

public sealed class GetSupplierDocumentFileHandler
    : IRequestHandler<
        GetSupplierDocumentFileQuery,
        SupplierDocumentFileResponse>
{
    private readonly ISupplierDocumentFileService _service;

    public GetSupplierDocumentFileHandler(
        ISupplierDocumentFileService service)
    {
        _service = service;
    }

    public Task<SupplierDocumentFileResponse> Handle(
        GetSupplierDocumentFileQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAsync(
            request.SupplierId,
            request.DocumentId,
            cancellationToken);
    }
}
