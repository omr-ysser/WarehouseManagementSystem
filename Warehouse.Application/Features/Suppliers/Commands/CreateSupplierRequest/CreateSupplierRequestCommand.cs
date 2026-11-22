using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.CreateSupplierOrderRequest;

public sealed record CreateSupplierRequestCommand(
    int SupplierId,
    CreateSupplierRequestInput Input)
    : IRequest<SupplierRequestResponse>;
