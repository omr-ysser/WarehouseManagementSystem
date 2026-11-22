using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.AddSupplierNote;

public sealed class AddSupplierNoteHandler
    : IRequestHandler<AddSupplierNoteCommand, SupplierNoteResponse>
{
    private readonly ISupplierService _supplierService;

    public AddSupplierNoteHandler(
        ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public Task<SupplierNoteResponse> Handle(
        AddSupplierNoteCommand request,
        CancellationToken cancellationToken)
    {
        return _supplierService.AddNoteAsync(
            request.SupplierId,
            request.Data,
            request.ActorAccountId,
            cancellationToken);
    }
}
