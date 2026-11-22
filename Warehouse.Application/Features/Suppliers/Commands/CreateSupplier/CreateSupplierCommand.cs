using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(
    CreateSupplierRequest Data,
    string ActorAccountId)
    : IRequest<SupplierResponse>;