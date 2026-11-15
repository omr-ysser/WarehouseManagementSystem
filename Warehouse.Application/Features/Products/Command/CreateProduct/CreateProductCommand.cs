using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string SKU,
    string? Description)
    : IRequest<ProductResponse>;
