using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.ChangeSupplierStatus;

public sealed record ChangeSupplierStatusCommand(
    int SupplierId,
    bool IsActive,
    string ActorAccountId)
    : IRequest<SupplierStatusResponse>;
