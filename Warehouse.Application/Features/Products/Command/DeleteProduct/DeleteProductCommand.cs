using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace Warehouse.Application.Features.Products.DeleteProduct;

public sealed record DeleteProductCommand(int Id) : IRequest;
