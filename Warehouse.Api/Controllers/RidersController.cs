using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Features.Riders.Commands;
using Warehouse.Application.Features.Riders.Queries;
using Warehouse.Domain.Entities;

namespace Warehouse.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/riders")]
public sealed class RidersController(ISender sender) : ControllerBase
{
    #region Get Riders

    [HttpGet]
    [Authorize(Policy = "Permission:Riders.View")]
    public async Task<ActionResult<PageResponse<RiderResponse>>> GetAll(
        CancellationToken ct,
        [FromQuery] string? search = null,
        [FromQuery] bool? available = null,
        [FromQuery] string[]? nationalities = null,
        [FromQuery, Range(-2, 10)] int? minHandicap = null,
        [FromQuery, Range(-2, 10)] int? maxHandicap = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        if (minHandicap.HasValue &&
            maxHandicap.HasValue &&
            minHandicap.Value > maxHandicap.Value)
        {
            ModelState.AddModelError(
                nameof(minHandicap),
                "Minimum handicap must not exceed maximum handicap.");

            return ValidationProblem(ModelState);
        }

        var result = await sender.Send(
      new GetRidersQuery(
          Search: search,
          Available: available,
          Page: page,
          PageSize: pageSize,
          Nationalities: nationalities,
          HandicapMin: minHandicap,
          HandicapMax: maxHandicap),
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

    #endregion

    #region Create Rider

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

    #endregion

    #region Update Rider

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

    #endregion

    #region Delete Rider

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permission:Riders.Delete")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery, Required] string rowVersion,
        CancellationToken ct)
    {
        await sender.Send(
            new DeleteRiderCommand(id, rowVersion),
            ct);

        return NoContent();
    }

    #endregion

    #region Change Rider Availability

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

    #endregion

    #region Get Rider Tournaments

    [HttpGet("{riderId:int}/tournaments")]
    [Authorize(Policy = "Permission:Riders.View")]
    public async Task<ActionResult<PageResponse<ParticipationResponse>>>
        GetTournaments(
            int riderId,
            CancellationToken ct,
            [FromQuery] string? search = null,
            [FromQuery] ParticipationStatus? status = null,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 20)
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

    #endregion

    #region Assign Tournament

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

    #endregion

    #region Update Tournament Horses

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

    #endregion

    #region Change Tournament Availability

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

    #endregion

    #region Change Tournament Status

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

    #endregion

    #region Remove Tournament

    [HttpDelete("{riderId:int}/tournaments/{tournamentId:int}")]
    [Authorize(Policy = "Permission:Riders.ManageTournaments")]
    public async Task<IActionResult> RemoveTournament(
        int riderId,
        int tournamentId,
        [FromQuery, Required] string rowVersion,
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

    #endregion
}