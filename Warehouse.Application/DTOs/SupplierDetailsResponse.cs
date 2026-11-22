using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed record SupplierNoteResponse(
    string Content,
    string? CreatedByAccountId,
    string? CreatedByName,
    DateTime? CreatedAtUtc);

public sealed record SupplierDetailsResponse(
    int Id,
    string Name,
    string? Email,
    bool IsActive,
    string CreatedByAccountId,
    DateTime CreatedAtUtc,
    SupplierContactResponse PrimaryContact,
    SupplierContactResponse? AdditionalContact1,
    SupplierContactResponse? AdditionalContact2,
    SupplierContactResponse? AdditionalContact3,
    IReadOnlyList<string> Trucks,
    IReadOnlyList<SupplierDocumentResponse> Documents,
    SupplierNoteResponse? Note);
