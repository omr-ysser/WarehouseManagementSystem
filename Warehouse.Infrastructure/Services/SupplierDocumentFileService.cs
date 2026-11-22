using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Services;

public sealed class SupplierDocumentFileService
    : ISupplierDocumentFileService
{
    private readonly ApplicationDbContext _db;

    public SupplierDocumentFileService(
        ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SupplierDocumentFileResponse> GetAsync(
        int supplierId,
        int documentId,
        CancellationToken ct)
    {
        var document = await _db.SupplierDocuments
            .AsNoTracking()
            .Where(item =>
                item.Id == documentId &&
                item.SupplierId == supplierId)
            .Select(item => new SupplierDocumentFileResponse(
                item.FileName,
                item.Content))
            .SingleOrDefaultAsync(ct);

        if (document is null)
        {
            // Same response for a missing document or incorrect supplier.
            throw new AppException(
                404,
                "Supplier document not found.");
        }

        return document;
    }
}
