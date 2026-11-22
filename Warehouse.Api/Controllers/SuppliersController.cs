using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Features.Suppliers.Commands.AddSupplierNote;
using Warehouse.Application.Features.Suppliers.Commands.CreateSupplier;
using Warehouse.Application.Features.Suppliers.Commands.UpdateSupplier;
using Warehouse.Application.Features.Suppliers.Queries.ExportSuppliers;
using Warehouse.Application.Features.Suppliers.Queries.GetSupplierById;
using Warehouse.Application.Features.Suppliers.Queries.ListSuppliers;
using Warehouse.Application.Features.Suppliers.Commands.ChangeSupplierStatus;
using CreateSupplierDto =
    global::Warehouse.Application.DTOs.CreateSupplierRequest;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
public sealed class SuppliersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Suppliers.View")]
    public async Task<ActionResult<SupplierListResponse>> List(
        [FromQuery] SupplierListParameters parameters,
        CancellationToken ct)
    {
        var query = new ListSuppliersQuery(parameters);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }

    [HttpGet("export")]
    [Authorize(Policy = "Permission:Suppliers.View")]
    [Authorize(Policy = "Permission:Suppliers.Export")]
    public async Task<IActionResult> Export(
        [FromQuery] SupplierListFilter filter,
        CancellationToken ct)
    {
        var query = new ExportSuppliersQuery(filter);
        var suppliers = await sender.Send(query, ct);

        var content = JsonSerializer.SerializeToUtf8Bytes(
            suppliers,
            JsonSerializerOptions.Web);

        return File(content, "application/json", "suppliers.json");
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:Suppliers.View")]
    public async Task<ActionResult<SupplierDetailsResponse>> GetById(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var query = new GetSupplierByIdQuery(id);
        var supplier = await sender.Send(query, ct);

        return Ok(supplier);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Suppliers.Create")]
    [Consumes("application/json")]
    public async Task<ActionResult<SupplierResponse>> Create(
        [FromBody] CreateSupplierDto request,
        CancellationToken ct)
    {
        var actorAccountId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            return Unauthorized();
        }

        var command = new CreateSupplierCommand(request, actorAccountId);
        var supplier = await sender.Send(command, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = supplier.Id },
            supplier);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "Permission:Suppliers.View")]
    [Authorize(Policy = "Permission:Suppliers.Edit")]
    public async Task<ActionResult<SupplierDetailsResponse>> Update(
      [FromRoute] int id,
      [FromBody] UpdateSupplierInput input,
      CancellationToken ct)
    {
        var actorAccountId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            return Unauthorized();
        }

        var command = new UpdateSupplierCommand(id, input, actorAccountId);
        var supplier = await sender.Send(command, ct);

        return Ok(supplier);
    }

    [HttpPost("{id:int}/note")]
    [Authorize(Policy = "Permission:Suppliers.AddNote")]
    [Consumes("application/json")]
    public async Task<ActionResult<SupplierNoteResponse>> AddNote(
        [FromRoute] int id,
        [FromBody] AddSupplierNoteRequest request,
        CancellationToken ct)
    {
        var actorAccountId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            return Unauthorized();
        }

        var command = new AddSupplierNoteCommand(id, request, actorAccountId);
        var note = await sender.Send(command, ct);

        return Ok(note);
    }
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "Permission:Suppliers.View")]
    [Authorize(Policy = "Permission:Suppliers.ChangeStatus")]
    public async Task<ActionResult<SupplierStatusResponse>> ChangeStatus(
    [FromRoute] int id,
    [FromBody] ChangeSupplierStatusInput input,
    CancellationToken ct)
    {
        var actorAccountId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            return Unauthorized();
        }

        if (input.IsActive is not bool isActive)
        {
            ModelState.AddModelError("isActive", "Supplier status is required.");
            return ValidationProblem(ModelState);
        }

        var command = new ChangeSupplierStatusCommand(id, isActive, actorAccountId);
        var result = await sender.Send(command, ct);

        return Ok(result);
    }
}