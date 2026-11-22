using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warehouse.Application.DTOs;
using Warehouse.Domain.Enums;

namespace Warehouse.Application.Abstractions;

public interface ISupplierDocumentService
{
    Task<SupplierDocumentResponse> UploadAsync(
        SupplierDocumentType type,
        SupplierDocumentUpload upload,
        string actorAccountId,
        CancellationToken ct);
}