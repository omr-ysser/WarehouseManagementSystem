namespace Warehouse.Application.DTOs;

public sealed record UpdateSupplierFinanceInput(
    decimal? CollectedAmount,
    decimal? Payments);