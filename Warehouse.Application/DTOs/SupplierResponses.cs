using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed record SupplierContactResponse(
    string Person,
    string PhoneNumber,
    string JobTitle);

public sealed record SupplierDocumentResponse(
    int Id,
    string Type,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record SupplierResponse(
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
    IReadOnlyList<SupplierDocumentResponse> Documents);
