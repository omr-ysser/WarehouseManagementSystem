using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.CreateSupplier;

public sealed class CreateSupplierHandler
    : IRequestHandler<CreateSupplierCommand, SupplierResponse>
{
    private readonly ISupplierService _supplierService;

    public CreateSupplierHandler(
        ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public Task<SupplierResponse> Handle(
        CreateSupplierCommand request,
        CancellationToken cancellationToken)
    {
        return _supplierService.CreateAsync(
            request.Data,
            request.ActorAccountId,
            cancellationToken);
    }
}