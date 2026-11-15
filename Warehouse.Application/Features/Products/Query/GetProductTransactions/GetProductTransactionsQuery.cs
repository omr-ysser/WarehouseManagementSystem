using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Products.GetProductTransactions;

public sealed record GetProductTransactionsQuery(int ProductId)
    : IRequest<IReadOnlyList<StockTransactionResponse>>;
