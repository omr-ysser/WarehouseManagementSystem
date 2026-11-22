using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Suppliers.Queries.GetSupplierById;

public sealed record GetSupplierByIdQuery(int Id)
    : IRequest<SupplierDetailsResponse>;
