using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;

namespace Warehouse.Application.Abstractions;

public interface ISupplierDocumentFileService
{
    Task<SupplierDocumentFileResponse> GetAsync(
        int supplierId,
        int documentId,
        CancellationToken ct);
}
