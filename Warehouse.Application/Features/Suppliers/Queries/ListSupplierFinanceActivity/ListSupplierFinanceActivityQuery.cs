using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.ListSupplierFinanceActivity;

public sealed record ListSupplierFinanceActivityQuery(
    int SupplierId,
    int RequestId)
    : IRequest<IReadOnlyList<SupplierFinanceActivityResponse>>;
