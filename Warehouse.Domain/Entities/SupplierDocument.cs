using Warehouse.Domain.Enums;

namespace Warehouse.Domain.Entities;

public sealed class SupplierDocument
{
    public int Id { get; set; }

    // Null until the document is attached to a supplier.
    public int? SupplierId { get; set; }

    public SupplierDocumentType Type { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long SizeBytes { get; set; }

    public byte[] Content { get; set; } = [];

    public string UploadedByAccountId { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public Supplier? Supplier { get; set; }
}