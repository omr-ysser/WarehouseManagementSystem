using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.ChangeSupplierStatus;

public sealed class ChangeSupplierStatusHandler(ISupplierService service)
    : IRequestHandler<ChangeSupplierStatusCommand, SupplierStatusResponse>
{
    public Task<SupplierStatusResponse> Handle(
        ChangeSupplierStatusCommand command,
        CancellationToken ct)
    {
        return service.ChangeStatusAsync(
            command.SupplierId,
            command.IsActive,
            command.ActorAccountId,
            ct);
    }
}