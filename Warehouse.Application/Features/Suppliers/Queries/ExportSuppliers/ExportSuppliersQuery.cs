using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.ExportSuppliers;

public sealed record ExportSuppliersQuery(SupplierListFilter Filter)
    : IRequest<IReadOnlyList<SupplierListItemResponse>>;
