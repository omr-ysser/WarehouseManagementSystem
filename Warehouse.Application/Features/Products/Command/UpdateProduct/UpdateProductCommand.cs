using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace Warehouse.Application.Features.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    int Id,
    string Name,
    string SKU,
    string? Description)
    : IRequest;
