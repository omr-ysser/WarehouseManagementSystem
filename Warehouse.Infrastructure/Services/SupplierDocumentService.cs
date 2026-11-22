using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UglyToad.PdfPig;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Services;

public sealed class SupplierDocumentService(ApplicationDbContext db)
    : ISupplierDocumentService
{
    private const long MaxFileBytes = 50L * 1024 * 1024;

    public async Task<SupplierDocumentResponse> UploadAsync(
        SupplierDocumentType type,
        SupplierDocumentUpload upload,
        string actorAccountId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            throw new AppException(401, "A valid authenticated user is required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new AppException(400, "Invalid supplier document type.");
        }

        if (upload.Length <= 0)
        {
            throw new AppException(400, "Please upload a non-empty document.");
        }

        if (upload.Length > MaxFileBytes)
        {
            throw new AppException(413, "The maximum document size is 50 MB.");
        }

        var fileName = Path.GetFileName(upload.FileName.Replace('\\', '/')).Trim();

        if (fileName.Length is 0 or > 255 ||
            fileName.Any(char.IsControl) ||
            !string.Equals(
                Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(400, "A valid PDF file name is required.");
        }

        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await upload.Content.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            if (output.Length + bytesRead > MaxFileBytes)
            {
                throw new AppException(413, "The maximum document size is 50 MB.");
            }

            output.Write(buffer, 0, bytesRead);
        }

        var content = output.ToArray();
        ValidatePdf(content);
        ct.ThrowIfCancellationRequested();

        var document = new SupplierDocument
        {
            SupplierId = null,
            Type = type,
            FileName = fileName,
            ContentType = "application/pdf",
            SizeBytes = content.LongLength,
            Content = content,
            UploadedByAccountId = actorAccountId,
            UploadedAtUtc = DateTime.UtcNow
        };

        db.SupplierDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        return new SupplierDocumentResponse(
            document.Id,
            document.Type.ToString(),
            document.FileName,
            document.ContentType,
            document.SizeBytes);
    }

    private static void ValidatePdf(byte[] content)
    {
        if (content.Length < 5 ||
            !content.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
        {
            throw new AppException(400, "The uploaded file must be a PDF.");
        }

        int pageCount;
        try
        {
            using var pdf = PdfDocument.Open(content);
            pageCount = pdf.NumberOfPages;
        }
        catch (Exception ex) when (
            ex is not OutOfMemoryException and not OperationCanceledException)
        {
            throw new AppException(400, "The uploaded PDF could not be opened.");
        }

        if (pageCount < 1)
        {
            throw new AppException(400, "The uploaded PDF must contain at least one page.");
        }
    }
}
