using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.UpdateSupplierFinance;

public sealed class UpdateSupplierFinanceHandler(ISupplierRequestService service)
    : IRequestHandler<UpdateSupplierFinanceCommand, SupplierRequestResponse>
{
    public Task<SupplierRequestResponse> Handle(
        UpdateSupplierFinanceCommand command,
        CancellationToken ct) =>
        service.UpdateFinanceAsync(
            command.SupplierId,
            command.RequestId,
            command.Input,
            command.ActorAccountId,
            ct);
}
