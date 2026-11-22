using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Features.Suppliers.Commands.CreateSupplierOrderRequest;
using Warehouse.Application.Features.Suppliers.Commands.UpdateSupplierFinance;
using Warehouse.Application.Features.Suppliers.Queries.GetSupplierRequest;
using Warehouse.Application.Features.Suppliers.Queries.ListSupplierFinanceActivity;
using Warehouse.Application.Features.Suppliers.Queries.ListSupplierRequests;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/suppliers/{supplierId:int}/requests")]
[Authorize(Policy = "Permission:Suppliers.View")]
[Authorize(Policy = "Permission:Requests.View")]
public sealed class SupplierRequestsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierRequestResponse>>> List(
        int supplierId,
        [FromQuery] SupplierRequestFilter filter,
        CancellationToken ct)
    {
        var query = new ListSupplierRequestsQuery(supplierId, filter);
        var requests = await sender.Send(query, ct);

        return Ok(requests);
    }

    [HttpGet("{requestId:int}")]
    public async Task<ActionResult<SupplierRequestResponse>> GetById(
        int supplierId,
        int requestId,
        CancellationToken ct)
    {
        var query = new GetSupplierRequestQuery(supplierId, requestId);
        var request = await sender.Send(query, ct);

        return Ok(request);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Requests.Create")]
    public async Task<ActionResult<SupplierRequestResponse>> Create(
     int supplierId,
     [FromBody] CreateSupplierRequestInput input,
     CancellationToken ct)
    {
        var command = new CreateSupplierRequestCommand(supplierId, input);
        var request = await sender.Send(command, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { supplierId, requestId = request.Id },
            request);
    }

    [HttpPatch("{requestId:int}/finance")]
    [Authorize(Policy = "Permission:Requests.EditFinance")]
    public async Task<ActionResult<SupplierRequestResponse>> UpdateFinance(
        int supplierId,
        int requestId,
        [FromBody] UpdateSupplierFinanceInput input,
        CancellationToken ct)
    {
        var actorAccountId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            return Unauthorized();
        }

        var command = new UpdateSupplierFinanceCommand(
            supplierId,
            requestId,
            input,
            actorAccountId);

        var request = await sender.Send(command, ct);

        return Ok(request);
    }

    [HttpGet("{requestId:int}/finance/activity")]
    [Authorize(Policy = "Permission:Requests.ViewFinanceActivity")]
    public async Task<ActionResult<IReadOnlyList<SupplierFinanceActivityResponse>>>
        ListFinanceActivity(
            int supplierId,
            int requestId,
            CancellationToken ct)
    {
        var query = new ListSupplierFinanceActivityQuery(supplierId, requestId);
        var activity = await sender.Send(query, ct);

        return Ok(activity);
    }

    [HttpGet("export")]
    [Authorize(Policy = "Permission:Suppliers.Export")]
    public async Task<IActionResult> Export(
        int supplierId,
        [FromQuery] SupplierRequestFilter filter,
        CancellationToken ct)
    {
        var query = new ListSupplierRequestsQuery(supplierId, filter);
        var requests = await sender.Send(query, ct);

        var rows = requests.Select(request => new
        {
            request.OrderId,
            request.FromDate,
            request.ToDate,
            request.LoadWeight,
            request.FulfillmentSource,
            request.CollectedAmount,
            request.Payments,
            request.TotalAmount
        });

        var content = JsonSerializer.SerializeToUtf8Bytes(
            rows,
            JsonSerializerOptions.Web);

        return File(
            content,
            "application/json",
            $"supplier-{supplierId}-requests.json");
    }
}