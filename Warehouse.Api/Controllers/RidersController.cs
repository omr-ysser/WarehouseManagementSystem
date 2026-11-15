using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Features.Riders.Commands;
using Warehouse.Application.Features.Riders.Queries;
using Warehouse.Domain.Entities;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/riders")]
public sealed class RidersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Riders.View")]
    public async Task<ActionResult<PageResponse<RiderResponse>>> GetAll(
        CancellationToken ct,
        [FromQuery] string? search = null,
        [FromQuery] bool? available = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await sender.Send(
            new GetRidersQuery(
                search,
                available,
                page,
                pageSize),
            ct);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:Riders.View")]
    public async Task<ActionResult<RiderResponse>> GetById(
        int id,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new GetRiderByIdQuery(id),
            ct);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Riders.Create")]
    public async Task<ActionResult<RiderResponse>> Create(
        [FromBody] RiderRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateRiderCommand(request),
            ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:Riders.Edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateRiderRequest request,
        CancellationToken ct)
    {
        await sender.Send(
            new UpdateRiderCommand(id, request),
            ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Riders.Delete")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] string rowVersion,
        CancellationToken ct)
    {
        await sender.Send(
            new DeleteRiderCommand(id, rowVersion),
            ct);

        return NoContent();
    }

    [HttpPatch("{id:int}/availability")]
    [Authorize(Policy = "Permission:Riders.ChangeStatus")]
    public async Task<IActionResult> ChangeAvailability(
        int id,
        [FromBody] AvailabilityRequest request,
        CancellationToken ct)
    {
        await sender.Send(
            new ChangeRiderAvailabilityCommand(id, request),
            ct);

        return NoContent();
    }

    [HttpGet("{riderId:int}/tournaments")]
    [Authorize(Policy = "Permission:Riders.View")]
    public async Task<ActionResult<PageResponse<ParticipationResponse>>>
        GetTournaments(
            int riderId,
            CancellationToken ct,
            [FromQuery] string? search = null,
            [FromQuery] ParticipationStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
    {
        var result = await sender.Send(
            new GetRiderTournamentsQuery(
                riderId,
                search,
                status,
                page,
                pageSize),
            ct);

        return Ok(result);
    }

    [HttpPost("{riderId:int}/tournaments/{tournamentId:int}")]
    [Authorize(Policy = "Permission:Riders.ManageTournaments")]
    public async Task<ActionResult<ParticipationResponse>> AssignTournament(
        int riderId,
        int tournamentId,
        [FromBody] ParticipationRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new AssignRiderToTournamentCommand(
                riderId,
                tournamentId,
                request),
            ct);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPut("{riderId:int}/tournaments/{tournamentId:int}/horses")]
    [Authorize(Policy = "Permission:Riders.ManageTournaments")]
    public async Task<IActionResult> UpdateTournamentHorses(
        int riderId,
        int tournamentId,
        [FromBody] UpdateParticipationRequest request,
        CancellationToken ct)
    {
        await sender.Send(
            new UpdateRiderTournamentHorsesCommand(
                riderId,
                tournamentId,
                request),
            ct);

        return NoContent();
    }

    [HttpPatch(
        "{riderId:int}/tournaments/{tournamentId:int}/availability")]
    [Authorize(Policy = "Permission:Riders.ManageTournaments")]
    public async Task<IActionResult> ChangeTournamentAvailability(
        int riderId,
        int tournamentId,
        [FromBody] AvailabilityRequest request,
        CancellationToken ct)
    {
        await sender.Send(
            new ChangeRiderTournamentAvailabilityCommand(
                riderId,
                tournamentId,
                request),
            ct);

        return NoContent();
    }

    [HttpPatch("{riderId:int}/tournaments/{tournamentId:int}/status")]
    [Authorize(Policy = "Permission:Riders.ManageTournaments")]
    public async Task<IActionResult> ChangeTournamentStatus(
        int riderId,
        int tournamentId,
        [FromBody] ParticipationStatusRequest request,
        CancellationToken ct)
    {
        await sender.Send(
            new ChangeRiderTournamentStatusCommand(
                riderId,
                tournamentId,
                request),
            ct);

        return NoContent();
    }

    [HttpDelete("{riderId:int}/tournaments/{tournamentId:int}")]
    [Authorize(Policy = "Permission:Riders.ManageTournaments")]
    public async Task<IActionResult> RemoveTournament(
        int riderId,
        int tournamentId,
        [FromQuery] string rowVersion,
        CancellationToken ct)
    {
        await sender.Send(
            new RemoveRiderFromTournamentCommand(
                riderId,
                tournamentId,
                rowVersion),
            ct);

        return NoContent();
    }
}
