using Warehouse.Application.DTOs;

namespace Warehouse.Application.Abstractions;

public interface ISupplierService
{
    Task<SupplierResponse> CreateAsync(
        CreateSupplierRequest request,
        string actorAccountId,
        CancellationToken ct);
    Task<SupplierDetailsResponse> GetByIdAsync(
    int id,
    CancellationToken ct);
    Task<SupplierNoteResponse> AddNoteAsync(
    int supplierId,
    AddSupplierNoteRequest request,
    string actorAccountId,
    CancellationToken ct);
    Task<SupplierListResponse> ListAsync(
    SupplierListParameters parameters,
    CancellationToken ct);

    Task<IReadOnlyList<SupplierListItemResponse>> ExportAsync(
        SupplierListFilter filter,
        CancellationToken ct);

    Task<SupplierDetailsResponse> UpdateAsync(
        int supplierId,
        UpdateSupplierInput input,
        string actorAccountId,
        CancellationToken ct);
    Task<SupplierStatusResponse> ChangeStatusAsync(
    int supplierId,
    bool isActive,
    string actorAccountId,
    CancellationToken ct);

}
