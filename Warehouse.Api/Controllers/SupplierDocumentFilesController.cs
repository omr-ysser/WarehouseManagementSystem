using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Features.Suppliers.Queries.GetSupplierDocumentFile;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/suppliers/{supplierId:int}/documents")]
public sealed class SupplierDocumentFilesController : ControllerBase
{
    private readonly ISender _sender;

    public SupplierDocumentFilesController(
        ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{documentId:int}")]
    [Authorize(Policy = "Permission:Suppliers.ViewDocuments")]
    [ProducesResponseType(
        typeof(FileContentResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(
        int supplierId,
        int documentId,
        CancellationToken ct,
        [FromQuery] bool download = false)
    {
        var document = await _sender.Send(
            new GetSupplierDocumentFileQuery(
                supplierId,
                documentId),
            ct);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "no-store";

        if (download)
        {
            return File(
                document.Content,
                "application/pdf",
                document.FileName);
        }

        return File(
            document.Content,
            "application/pdf");
    }
}
