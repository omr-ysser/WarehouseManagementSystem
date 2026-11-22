using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed record SupplierListResponse(
    IReadOnlyList<SupplierListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
