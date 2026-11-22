using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;
using Warehouse.Infrastructure.Persistence;
using SupplierCreationInput =
    global::Warehouse.Application.DTOs.CreateSupplierRequest;

namespace Warehouse.Infrastructure.Services;

public sealed class SupplierService(
    ApplicationDbContext db,
    IValidator<SupplierCreationInput> validator,
    IValidator<AddSupplierNoteRequest> noteValidator) : ISupplierService
{
    public async Task<SupplierResponse> CreateAsync(
        SupplierCreationInput request,
        string actorAccountId,
        CancellationToken ct)
    {
        if (request is null)
        {
            throw new AppException(400, "Supplier information is required.");
        }

        EnsureAuthenticated(actorAccountId);

        await validator.ValidateAndThrowAsync(request, ct);

        await using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var taxDocument = FindPendingDocument(
            request.TaxNumberDocumentId,
            SupplierDocumentType.TaxNumber,
            actorAccountId);

        var commercialDocument = FindPendingDocument(
            request.CommercialRegistrationDocumentId,
            SupplierDocumentType.CommercialRegistration,
            actorAccountId);

        var documents = await taxDocument
            .Concat(commercialDocument)
            .OrderBy(document => document.Id)
            .Select(document => new SupplierDocumentResponse(
                document.Id,
                document.Type.ToString(),
                document.FileName,
                document.ContentType,
                document.SizeBytes))
            .ToArrayAsync(ct);

        if (documents.Length != 2)
        {
            throw new AppException(
                400,
                "Upload both required PDFs with the current account before creating the supplier.");
        }

        var supplier = new Supplier
        {
            IsActive = true,
            CreatedByAccountId = actorAccountId,
            CreatedAtUtc = DateTime.UtcNow,
            Trucks = request.Trucks
                .Select(value => new SupplierTruck { Value = value.Trim() })
                .ToList()
        };

        SetSupplierInformation(supplier, request);

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);

        await AttachDocumentAsync(taxDocument, supplier.Id, ct);
        await AttachDocumentAsync(commercialDocument, supplier.Id, ct);

        var response = new SupplierResponse(
            supplier.Id,
            supplier.Name,
            supplier.Email,
            supplier.IsActive,
            supplier.CreatedByAccountId,
            supplier.CreatedAtUtc,
            MapContact(supplier.PrimaryContact)!,
            MapContact(supplier.AdditionalContact1),
            MapContact(supplier.AdditionalContact2),
            MapContact(supplier.AdditionalContact3),
            supplier.Trucks.Select(truck => truck.Value).ToArray(),
            documents);

        await transaction.CommitAsync(ct);

        return response;
    }

    public async Task<SupplierListResponse> ListAsync(
        SupplierListParameters parameters,
        CancellationToken ct)
    {
        if (parameters.Page is < 1 or > 1_000_000 ||
            parameters.PageSize is < 1 or > 100)
        {
            throw new AppException(400, "Invalid page or page size.");
        }

        var query = QueryListItems(parameters);
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToArrayAsync(ct);

        return new SupplierListResponse(
            items,
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<SupplierListItemResponse>> ExportAsync(
        SupplierListFilter filter,
        CancellationToken ct)
    {
        return await QueryListItems(filter).ToArrayAsync(ct);
    }

    public async Task<SupplierDetailsResponse> GetByIdAsync(
        int id,
        CancellationToken ct)
    {
        var supplier = await db.Suppliers
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new SupplierDetailsResponse(
                item.Id,
                item.Name,
                item.Email,
                item.IsActive,
                item.CreatedByAccountId,
                item.CreatedAtUtc,
                new SupplierContactResponse(
                    item.PrimaryContact.Person,
                    item.PrimaryContact.PhoneNumber,
                    item.PrimaryContact.JobTitle),
                item.AdditionalContact1 == null
                    ? null
                    : new SupplierContactResponse(
                        item.AdditionalContact1.Person,
                        item.AdditionalContact1.PhoneNumber,
                        item.AdditionalContact1.JobTitle),
                item.AdditionalContact2 == null
                    ? null
                    : new SupplierContactResponse(
                        item.AdditionalContact2.Person,
                        item.AdditionalContact2.PhoneNumber,
                        item.AdditionalContact2.JobTitle),
                item.AdditionalContact3 == null
                    ? null
                    : new SupplierContactResponse(
                        item.AdditionalContact3.Person,
                        item.AdditionalContact3.PhoneNumber,
                        item.AdditionalContact3.JobTitle),
                item.Trucks
                    .OrderBy(truck => truck.Value)
                    .Select(truck => truck.Value)
                    .ToArray(),
                item.Documents
                    .OrderBy(document => document.Id)
                    .Select(document => new SupplierDocumentResponse(
                        document.Id,
                        document.Type.ToString(),
                        document.FileName,
                        document.ContentType,
                        document.SizeBytes))
                    .ToArray(),
                item.Note == null
                    ? null
                    : new SupplierNoteResponse(
                        item.Note,
                        item.NoteCreatedByAccountId,
                        db.Users
                            .Where(user => user.Id == item.NoteCreatedByAccountId)
                            .Select(user => user.FullName)
                            .FirstOrDefault(),
                        item.NoteCreatedAtUtc)))
            .SingleOrDefaultAsync(ct);

        return supplier ?? throw new AppException(404, "Supplier not found.");
    }

    public async Task<SupplierDetailsResponse> UpdateAsync(
        int supplierId,
        UpdateSupplierInput input,
        string actorAccountId,
        CancellationToken ct)
    {
        if (input is null)
        {
            throw new AppException(400, "Supplier information is required.");
        }

        EnsureAuthenticated(actorAccountId);

        var fields = new SupplierCreationInput
        {
            Name = input.Name,
            Email = input.Email,
            PrimaryContact = input.PrimaryContact,
            AdditionalContact1 = input.AdditionalContact1,
            AdditionalContact2 = input.AdditionalContact2,
            AdditionalContact3 = input.AdditionalContact3,
            Trucks = input.Trucks
        };

        var validation = await validator.ValidateAsync(
            fields,
            options => options.IncludeProperties(
                request => request.Name,
                request => request.Email,
                request => request.PrimaryContact,
                request => request.AdditionalContact1,
                request => request.AdditionalContact2,
                request => request.AdditionalContact3,
                request => request.Trucks),
            ct);

        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        await using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var supplier = await db.Suppliers
            .Include(item => item.Trucks)
            .SingleOrDefaultAsync(item => item.Id == supplierId, ct)
            ?? throw new AppException(404, "Supplier not found.");

        await ReplaceRequiredDocumentAsync(
            supplierId,
            SupplierDocumentType.TaxNumber,
            input.TaxNumberDocumentId,
            actorAccountId,
            ct);

        await ReplaceRequiredDocumentAsync(
            supplierId,
            SupplierDocumentType.CommercialRegistration,
            input.CommercialRegistrationDocumentId,
            actorAccountId,
            ct);

        SetSupplierInformation(supplier, fields);

        var requestedTrucks = fields.Trucks
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.Ordinal);

        supplier.Trucks.RemoveAll(truck => !requestedTrucks.Contains(truck.Value));

        // Delete removed rows first so case-only edits work with a
        // case-insensitive SQL Server unique index.
        await db.SaveChangesAsync(ct);

        requestedTrucks.ExceptWith(supplier.Trucks.Select(truck => truck.Value));

        supplier.Trucks.AddRange(
            requestedTrucks.Select(value => new SupplierTruck { Value = value }));

        await db.SaveChangesAsync(ct);

        var response = await GetByIdAsync(supplierId, ct);
        await transaction.CommitAsync(ct);

        return response;
    }

    public async Task<SupplierStatusResponse> ChangeStatusAsync(
        int supplierId,
        bool isActive,
        string actorAccountId,
        CancellationToken ct)
    {
        EnsureAuthenticated(actorAccountId);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var supplierQuery = db.Suppliers
            .Where(supplier => supplier.Id == supplierId);

        var affectedRows = await supplierQuery
            .Where(supplier => supplier.IsActive != isActive)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    supplier => supplier.IsActive,
                    isActive),
                ct);

        if (affectedRows == 0)
        {
            var currentStatus = await supplierQuery
                .Select(supplier => (bool?)supplier.IsActive)
                .SingleOrDefaultAsync(ct);

            if (currentStatus is null)
            {
                throw new AppException(404, "Supplier not found.");
            }

            if (currentStatus.Value != isActive)
            {
                throw new AppException(
                    409,
                    "The supplier status changed concurrently. Reload and try again.");
            }
        }
        else
        {
            db.SupplierStatusActivities.Add(new SupplierStatusActivity
            {
                SupplierId = supplierId,
                PreviousIsActive = !isActive,
                NewIsActive = isActive,
                ActorAccountId = actorAccountId,
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        return new SupplierStatusResponse(supplierId, isActive);
    }

    public async Task<SupplierNoteResponse> AddNoteAsync(
        int supplierId,
        AddSupplierNoteRequest request,
        string actorAccountId,
        CancellationToken ct)
    {
        if (request is null)
        {
            throw new AppException(400, "Note information is required.");
        }

        EnsureAuthenticated(actorAccountId);

        await noteValidator.ValidateAndThrowAsync(request, ct);

        var content = request.Content.Trim();
        var createdAtUtc = DateTime.UtcNow;

        var creatorName = await db.Users
            .AsNoTracking()
            .Where(user => user.Id == actorAccountId)
            .Select(user => user.FullName)
            .FirstOrDefaultAsync(ct);

        var updatedCount = await db.Suppliers
            .Where(supplier => supplier.Id == supplierId && supplier.Note == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(supplier => supplier.Note, content)
                    .SetProperty(
                        supplier => supplier.NoteCreatedByAccountId,
                        actorAccountId)
                    .SetProperty(
                        supplier => supplier.NoteCreatedAtUtc,
                        (DateTime?)createdAtUtc),
                ct);

        if (updatedCount == 0)
        {
            var supplierExists = await db.Suppliers
                .AnyAsync(supplier => supplier.Id == supplierId, ct);

            if (!supplierExists)
            {
                throw new AppException(404, "Supplier not found.");
            }

            throw new AppException(409, "This supplier already has a note.");
        }

        return new SupplierNoteResponse(
            content,
            actorAccountId,
            creatorName,
            createdAtUtc);
    }

    private IQueryable<SupplierListItemResponse> QueryListItems(
        SupplierListFilter filter)
    {
        var suppliers = db.Suppliers.AsNoTracking();

        suppliers = filter.Status?.Trim().ToLowerInvariant() switch
        {
            null or "" or "all" => suppliers,
            "active" => suppliers.Where(supplier => supplier.IsActive),
            "inactive" => suppliers.Where(supplier => !supplier.IsActive),
            _ => throw new AppException(
                400,
                "Status must be All, Active, or Inactive.")
        };

        var search = filter.Search?.Trim();

        if (!string.IsNullOrEmpty(search))
        {
            suppliers = suppliers.Where(supplier =>
                supplier.Name.Contains(search) ||
                (supplier.Email != null && supplier.Email.Contains(search)));
        }

        var descending = filter.SortDirection?.Trim().ToLowerInvariant() switch
        {
            null or "" or "asc" => false,
            "desc" => true,
            _ => throw new AppException(400, "Sort direction must be asc or desc.")
        };

        var sorted = filter.SortBy?.Trim().ToLowerInvariant() switch
        {
            null or "" or "name" => descending
                ? suppliers.OrderByDescending(supplier => supplier.Name)
                : suppliers.OrderBy(supplier => supplier.Name),

            "email" => descending
                ? suppliers.OrderByDescending(supplier => supplier.Email)
                : suppliers.OrderBy(supplier => supplier.Email),

            "truckcount" => descending
                ? suppliers.OrderByDescending(supplier => supplier.Trucks.Count)
                : suppliers.OrderBy(supplier => supplier.Trucks.Count),

            "status" or "isactive" => descending
                ? suppliers.OrderBy(supplier => supplier.IsActive)
                : suppliers.OrderByDescending(supplier => supplier.IsActive),

            _ => throw new AppException(400, "Unsupported supplier sort field.")
        };

        return sorted
            .ThenBy(supplier => supplier.Id)
            .Select(supplier => new SupplierListItemResponse(
                supplier.Id,
                supplier.Name,
                supplier.Email,
                supplier.Trucks.Count,
                supplier.IsActive));
    }

    private async Task ReplaceRequiredDocumentAsync(
        int supplierId,
        SupplierDocumentType type,
        int? requestedId,
        string actorAccountId,
        CancellationToken ct)
    {
        var currentDocuments = db.SupplierDocuments.Where(document =>
            document.SupplierId == supplierId && document.Type == type);

        var currentDocument = await currentDocuments
            .Select(document => new
            {
                document.Id,
                document.SizeBytes,
                document.ContentType
            })
            .SingleOrDefaultAsync(ct);

        if (requestedId is null || requestedId == currentDocument?.Id)
        {
            if (currentDocument is null ||
                currentDocument.SizeBytes <= 0 ||
                currentDocument.ContentType != "application/pdf")
            {
                throw new AppException(400, $"A valid {type} document is required.");
            }

            return;
        }

        var replacement = FindPendingDocument(requestedId.Value, type, actorAccountId);

        if (!await replacement.AnyAsync(ct))
        {
            throw new AppException(
                400,
                $"Upload a valid {type} replacement with the current account.");
        }

        // The unique index permits one attached document per supplier and type.
        await currentDocuments.ExecuteDeleteAsync(ct);
        await AttachDocumentAsync(replacement, supplierId, ct);
    }

    private IQueryable<SupplierDocument> FindPendingDocument(
        int id,
        SupplierDocumentType type,
        string actorAccountId)
    {
        return db.SupplierDocuments.Where(document =>
            document.Id == id &&
            document.SupplierId == null &&
            document.Type == type &&
            document.UploadedByAccountId == actorAccountId &&
            document.SizeBytes > 0 &&
            document.ContentType == "application/pdf");
    }

    private static async Task AttachDocumentAsync(
        IQueryable<SupplierDocument> documents,
        int supplierId,
        CancellationToken ct)
    {
        var attachedCount = await documents.ExecuteUpdateAsync(
            setters => setters.SetProperty(
                document => document.SupplierId,
                (int?)supplierId),
            ct);

        if (attachedCount != 1)
        {
            throw new AppException(409, "The document is no longer available.");
        }
    }

    private static void SetSupplierInformation(
        Supplier supplier,
        SupplierCreationInput request)
    {
        supplier.Name = request.Name.Trim();
        supplier.Email = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : request.Email.Trim();

        supplier.PrimaryContact = CreateContact(request.PrimaryContact)!;
        supplier.AdditionalContact1 = CreateContact(request.AdditionalContact1);
        supplier.AdditionalContact2 = CreateContact(request.AdditionalContact2);
        supplier.AdditionalContact3 = CreateContact(request.AdditionalContact3);
    }

    private static SupplierContact? CreateContact(SupplierContactRequest? request)
    {
        if (request is null)
        {
            return null;
        }

        return new SupplierContact
        {
            Person = request.Person.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            JobTitle = request.JobTitle.Trim()
        };
    }

    private static SupplierContactResponse? MapContact(SupplierContact? contact)
    {
        return contact is null
            ? null
            : new SupplierContactResponse(
                contact.Person,
                contact.PhoneNumber,
                contact.JobTitle);
    }

    private static void EnsureAuthenticated(string actorAccountId)
    {
        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            throw new AppException(401, "A valid authenticated user is required.");
        }
    }
}