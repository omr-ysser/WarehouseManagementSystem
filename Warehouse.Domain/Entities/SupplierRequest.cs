using Warehouse.Domain.Enums;

namespace Warehouse.Domain.Entities;

public sealed class SupplierRequest
{
    public int Id { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public string OrderId { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal LoadWeight { get; set; }
    public SupplierFulfillmentSource FulfillmentSource { get; set; }
    public string Status { get; set; } = "Pending";

    public decimal? CollectedAmount { get; set; }
    public decimal? Payments { get; set; }
}