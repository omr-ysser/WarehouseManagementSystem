using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Riders.Commands;

public sealed class CreateRiderHandler(IRiderService service)
    : IRequestHandler<CreateRiderCommand, RiderResponse>
{
    public Task<RiderResponse> Handle(
        CreateRiderCommand request,
        CancellationToken cancellationToken)
    {
        return service.CreateRiderAsync(
            request.Profile,
            cancellationToken);
    }
}

public sealed class UpdateRiderHandler(IRiderService service)
    : IRequestHandler<UpdateRiderCommand>
{
    public Task Handle(
        UpdateRiderCommand request,
        CancellationToken cancellationToken)
    {
        return service.UpdateRiderAsync(
            request.RiderId,
            request.Request,
            cancellationToken);
    }
}

public sealed class DeleteRiderHandler(IRiderService service)
    : IRequestHandler<DeleteRiderCommand>
{
    public Task Handle(
        DeleteRiderCommand request,
        CancellationToken cancellationToken)
    {
        return service.DeleteRiderAsync(
            request.RiderId,
            request.RowVersion,
            cancellationToken);
    }
}

public sealed class ChangeRiderAvailabilityHandler(IRiderService service)
    : IRequestHandler<ChangeRiderAvailabilityCommand>
{
    public Task Handle(
        ChangeRiderAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        return service.SetRiderAvailabilityAsync(
            request.RiderId,
            request.Request,
            cancellationToken);
    }
}

public sealed class AssignRiderToTournamentHandler(IRiderService service)
    : IRequestHandler<
        AssignRiderToTournamentCommand,
        ParticipationResponse>
{
    public Task<ParticipationResponse> Handle(
        AssignRiderToTournamentCommand request,
        CancellationToken cancellationToken)
    {
        return service.AssignAsync(
            request.RiderId,
            request.TournamentId,
            request.Request,
            cancellationToken);
    }
}

public sealed class UpdateRiderTournamentHorsesHandler(
    IRiderService service)
    : IRequestHandler<UpdateRiderTournamentHorsesCommand>
{
    public Task Handle(
        UpdateRiderTournamentHorsesCommand request,
        CancellationToken cancellationToken)
    {
        return service.UpdateHorsesAsync(
            request.RiderId,
            request.TournamentId,
            request.Request,
            cancellationToken);
    }
}

public sealed class ChangeRiderTournamentAvailabilityHandler(
    IRiderService service)
    : IRequestHandler<ChangeRiderTournamentAvailabilityCommand>
{
    public Task Handle(
        ChangeRiderTournamentAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        return service.SetParticipationAvailabilityAsync(
            request.RiderId,
            request.TournamentId,
            request.Request,
            cancellationToken);
    }
}

public sealed class ChangeRiderTournamentStatusHandler(
    IRiderService service)
    : IRequestHandler<ChangeRiderTournamentStatusCommand>
{
    public Task Handle(
        ChangeRiderTournamentStatusCommand request,
        CancellationToken cancellationToken)
    {
        return service.SetParticipationStatusAsync(
            request.RiderId,
            request.TournamentId,
            request.Request,
            cancellationToken);
    }
}

public sealed class RemoveRiderFromTournamentHandler(
    IRiderService service)
    : IRequestHandler<RemoveRiderFromTournamentCommand>
{
    public Task Handle(
        RemoveRiderFromTournamentCommand request,
        CancellationToken cancellationToken)
    {
        return service.RemoveParticipationAsync(
            request.RiderId,
            request.TournamentId,
            request.RowVersion,
            cancellationToken);
    }
}
