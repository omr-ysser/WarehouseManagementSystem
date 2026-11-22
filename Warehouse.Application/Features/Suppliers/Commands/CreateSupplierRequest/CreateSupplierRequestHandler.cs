using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.CreateSupplierOrderRequest;

public sealed class CreateSupplierRequestHandler(ISupplierRequestService service)
    : IRequestHandler<CreateSupplierRequestCommand, SupplierRequestResponse>
{
    public Task<SupplierRequestResponse> Handle(
        CreateSupplierRequestCommand command,
        CancellationToken ct)
    {
        return service.CreateAsync(command.SupplierId, command.Input, ct);
    }
}
