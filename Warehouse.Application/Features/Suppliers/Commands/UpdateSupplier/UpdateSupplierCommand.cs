using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(
    int SupplierId,
    UpdateSupplierInput Input,
    string ActorAccountId)
    : IRequest<SupplierDetailsResponse>;
