using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Features.Riders.Commands;

// Rider profile.

public sealed record CreateRiderCommand(RiderRequest Profile)
    : IRequest<RiderResponse>;

public sealed record UpdateRiderCommand(
    int RiderId,
    UpdateRiderRequest Request)
    : IRequest;

public sealed record DeleteRiderCommand(
    int RiderId,
    string RowVersion)
    : IRequest;

public sealed record ChangeRiderAvailabilityCommand(
    int RiderId,
    AvailabilityRequest Request)
    : IRequest;

// Rider tournament participation.

public sealed record AssignRiderToTournamentCommand(
    int RiderId,
    int TournamentId,
    ParticipationRequest Request)
    : IRequest<ParticipationResponse>;

public sealed record UpdateRiderTournamentHorsesCommand(
    int RiderId,
    int TournamentId,
    UpdateParticipationRequest Request)
    : IRequest;

public sealed record ChangeRiderTournamentAvailabilityCommand(
    int RiderId,
    int TournamentId,
    AvailabilityRequest Request)
    : IRequest;

public sealed record ChangeRiderTournamentStatusCommand(
    int RiderId,
    int TournamentId,
    ParticipationStatusRequest Request)
    : IRequest;

public sealed record RemoveRiderFromTournamentCommand(
    int RiderId,
    int TournamentId,
    string RowVersion)
    : IRequest;
