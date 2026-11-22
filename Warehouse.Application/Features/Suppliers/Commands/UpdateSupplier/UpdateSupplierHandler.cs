using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.UpdateSupplier;

public sealed class UpdateSupplierHandler(ISupplierService service)
    : IRequestHandler<UpdateSupplierCommand, SupplierDetailsResponse>
{
    public Task<SupplierDetailsResponse> Handle(
        UpdateSupplierCommand command,
        CancellationToken ct)
    {
        return service.UpdateAsync(
            command.SupplierId,
            command.Input,
            command.ActorAccountId,
            ct);
    }
}
