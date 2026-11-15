using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Products.GetProductById;

public sealed record GetProductByIdQuery(int Id)
    : IRequest<ProductResponse>;
