using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Commands.UpdateSupplierFinance;

public sealed record UpdateSupplierFinanceCommand(
    int SupplierId,
    int RequestId,
    UpdateSupplierFinanceInput Input,
    string ActorAccountId)
    : IRequest<SupplierRequestResponse>;
