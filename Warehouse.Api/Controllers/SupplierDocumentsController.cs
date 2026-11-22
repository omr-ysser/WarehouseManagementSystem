using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Features.Suppliers.Queries.GetSupplierById;
using Warehouse.Domain.Enums;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/supplier-documents")]
public sealed class SupplierDocumentsController(
    ISupplierDocumentService documentService,
    ISender sender) : ControllerBase
{
    private const long MaxFileBytes = 50L * 1024 * 1024;
    private const long MaxRequestBytes = 55L * 1024 * 1024;

    [HttpPost]
    [Authorize(Policy = "Permission:Suppliers.Create")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public Task<ActionResult<SupplierDocumentResponse>> Upload(
        [FromForm] SupplierDocumentType type,
        IFormFile? file,
        CancellationToken ct)
    {
        return UploadPdfAsync(type, file, ct);
    }

    [HttpPost("/api/suppliers/{supplierId:int}/documents")]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "Permission:Suppliers.View")]
    [Authorize(Policy = "Permission:Suppliers.Edit")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<ActionResult<SupplierDocumentResponse>> UploadReplacement(
        [FromRoute] int supplierId,
        [FromForm] SupplierDocumentType type,
        IFormFile? file,
        CancellationToken ct)
    {
        await sender.Send(new GetSupplierByIdQuery(supplierId), ct);

        return await UploadPdfAsync(type, file, ct);
    }

    private async Task<ActionResult<SupplierDocumentResponse>> UploadPdfAsync(
        SupplierDocumentType type,
        IFormFile? file,
        CancellationToken ct)
    {
        var actorAccountId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            return Unauthorized();
        }

        if (!Enum.IsDefined(type))
        {
            ModelState.AddModelError(
                nameof(type),
                "Please select a valid document type.");
        }

        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(
                nameof(file),
                "Please upload a non-empty document.");

            return ValidationProblem(ModelState);
        }

        if (file.Length > MaxFileBytes)
        {
            return Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Document is too large.",
                detail: "The maximum document size is 50 MB.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await using var stream = file.OpenReadStream();

        var upload = new SupplierDocumentUpload(
            FileName: file.FileName,
            Length: file.Length,
            Content: stream);

        var document = await documentService.UploadAsync(
            type,
            upload,
            actorAccountId,
            ct);

        return StatusCode(StatusCodes.Status201Created, document);
    }
}