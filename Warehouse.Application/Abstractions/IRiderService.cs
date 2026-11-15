using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Abstractions;

public interface IRiderService
{
    Task<PageResponse<RiderResponse>> GetRidersAsync(
        string? search, bool? available,
        int page, int pageSize, CancellationToken ct);

    Task<RiderResponse> GetRiderAsync(int id, CancellationToken ct);

    Task<RiderResponse> CreateRiderAsync(
        RiderRequest request, CancellationToken ct);

    Task UpdateRiderAsync(
        int id, UpdateRiderRequest request, CancellationToken ct);

    Task DeleteRiderAsync(
        int id, string rowVersion, CancellationToken ct);

    Task SetRiderAvailabilityAsync(
        int id, AvailabilityRequest request, CancellationToken ct);

    Task<PageResponse<TournamentResponse>> GetTournamentsAsync(
        string? search, int page, int pageSize, CancellationToken ct);

    Task<TournamentResponse> GetTournamentAsync(
        int id, CancellationToken ct);

    Task<TournamentResponse> CreateTournamentAsync(
        TournamentRequest request, CancellationToken ct);

    Task UpdateTournamentAsync(
        int id, UpdateTournamentRequest request, CancellationToken ct);

    Task DeleteTournamentAsync(
        int id, string rowVersion, CancellationToken ct);

    Task<PageResponse<ParticipationResponse>> GetParticipationsAsync(
        int riderId, string? search, ParticipationStatus? status,
        int page, int pageSize, CancellationToken ct);

    Task<ParticipationResponse> AssignAsync(
        int riderId, int tournamentId,
        ParticipationRequest request, CancellationToken ct);

    Task UpdateHorsesAsync(
        int riderId, int tournamentId,
        UpdateParticipationRequest request, CancellationToken ct);

    Task SetParticipationAvailabilityAsync(
        int riderId, int tournamentId,
        AvailabilityRequest request, CancellationToken ct);

    Task SetParticipationStatusAsync(
        int riderId, int tournamentId,
        ParticipationStatusRequest request, CancellationToken ct);

    Task RemoveParticipationAsync(
        int riderId, int tournamentId,
        string rowVersion, CancellationToken ct);
}
