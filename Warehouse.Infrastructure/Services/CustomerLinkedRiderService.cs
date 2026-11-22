using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Services;

public sealed class CustomerLinkedRiderService : IRiderService
{
    private readonly RiderService _riders;
    private readonly ICustomerAccountService _accounts;
    private readonly ApplicationDbContext _db;
    private readonly ICustomerPasswordService _passwords;

    public CustomerLinkedRiderService(
    RiderService riders,
    ICustomerAccountService accounts,
    ICustomerPasswordService passwords,
    ApplicationDbContext db)
    {
        _riders = riders;
        _accounts = accounts;
        _passwords = passwords;
        _db = db;
    }

    #region Rider Queries

    public Task<PageResponse<RiderResponse>> GetRidersAsync(
        string? search,
        bool? available,
        int page,
        int pageSize,
        CancellationToken ct,
        string[]? nationalities = null,
        int? handicapMin = null,
        int? handicapMax = null)
    {
        return _riders.GetRidersAsync(
            search,
            available,
            page,
            pageSize,
            ct,
            nationalities,
            handicapMin,
            handicapMax);
    }

    public Task<RiderResponse> GetRiderAsync(
        int id,
        CancellationToken ct)
    {
        return _riders.GetRiderAsync(id, ct);
    }

    #endregion

    #region Rider Commands

    public async Task<RiderResponse> CreateRiderAsync(
     RiderRequest request,
     CancellationToken ct)
    {
        await using var transaction = await _db.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        var response = await _riders.CreateRiderAsync(
            request,
            ct);

        var rider = await _db.Riders.SingleAsync(
            item => item.Id == response.Id,
            ct);

        rider.AccountId = await _accounts.CreateAccountAsync(
            rider,
            ct);

        await _db.SaveChangesAsync(ct);

        // Outbox entry is committed with the customer and account.
        await _passwords.QueueInvitationAsync(
            rider.Id,
            ct);

        await transaction.CommitAsync(ct);

        return await _riders.GetRiderAsync(
            rider.Id,
            ct);
    }

    public async Task UpdateRiderAsync(
        int id,
        UpdateRiderRequest request,
        CancellationToken ct)
    {
        await using var transaction = await _db.Database
            .BeginTransactionAsync(ct);

        await _riders.UpdateRiderAsync(
            id,
            request,
            ct);

        var rider = await _db.Riders.SingleAsync(
            item => item.Id == id,
            ct);

        await _accounts.SynchronizeNameAsync(
            rider,
            ct);

        await transaction.CommitAsync(ct);
    }

    public async Task DeleteRiderAsync(
        int id,
        string rowVersion,
        CancellationToken ct)
    {
        var hasLinkedAccount = await _db.Riders
            .AnyAsync(
                rider =>
                    rider.Id == id &&
                    rider.AccountId != null,
                ct);

        if (hasLinkedAccount)
        {
            throw new AppException(
                409,
                "This customer has a linked account. Disable the customer instead.");
        }

        await _riders.DeleteRiderAsync(
            id,
            rowVersion,
            ct);
    }

    public Task SetRiderAvailabilityAsync(
        int id,
        AvailabilityRequest request,
        CancellationToken ct)
    {
        // Login and token validation read this status from the database.
        return _riders.SetRiderAvailabilityAsync(
            id,
            request,
            ct);
    }

    #endregion

    #region Tournament Queries

    public Task<PageResponse<TournamentResponse>> GetTournamentsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        return _riders.GetTournamentsAsync(
            search,
            page,
            pageSize,
            ct);
    }

    public Task<TournamentResponse> GetTournamentAsync(
        int id,
        CancellationToken ct)
    {
        return _riders.GetTournamentAsync(id, ct);
    }

    #endregion

    #region Tournament Commands

    public Task<TournamentResponse> CreateTournamentAsync(
        TournamentRequest request,
        CancellationToken ct)
    {
        return _riders.CreateTournamentAsync(request, ct);
    }

    public Task UpdateTournamentAsync(
        int id,
        UpdateTournamentRequest request,
        CancellationToken ct)
    {
        return _riders.UpdateTournamentAsync(id, request, ct);
    }

    public Task DeleteTournamentAsync(
        int id,
        string rowVersion,
        CancellationToken ct)
    {
        return _riders.DeleteTournamentAsync(id, rowVersion, ct);
    }

    #endregion

    #region Participation Queries

    public Task<PageResponse<ParticipationResponse>> GetParticipationsAsync(
        int riderId,
        string? search,
        ParticipationStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        return _riders.GetParticipationsAsync(
            riderId,
            search,
            status,
            page,
            pageSize,
            ct);
    }

    #endregion

    #region Participation Commands

    public Task<ParticipationResponse> AssignAsync(
        int riderId,
        int tournamentId,
        ParticipationRequest request,
        CancellationToken ct)
    {
        return _riders.AssignAsync(
            riderId,
            tournamentId,
            request,
            ct);
    }

    public Task UpdateHorsesAsync(
        int riderId,
        int tournamentId,
        UpdateParticipationRequest request,
        CancellationToken ct)
    {
        return _riders.UpdateHorsesAsync(
            riderId,
            tournamentId,
            request,
            ct);
    }

    public Task SetParticipationAvailabilityAsync(
        int riderId,
        int tournamentId,
        AvailabilityRequest request,
        CancellationToken ct)
    {
        return _riders.SetParticipationAvailabilityAsync(
            riderId,
            tournamentId,
            request,
            ct);
    }

    public Task SetParticipationStatusAsync(
        int riderId,
        int tournamentId,
        ParticipationStatusRequest request,
        CancellationToken ct)
    {
        return _riders.SetParticipationStatusAsync(
            riderId,
            tournamentId,
            request,
            ct);
    }

    public Task RemoveParticipationAsync(
        int riderId,
        int tournamentId,
        string rowVersion,
        CancellationToken ct)
    {
        return _riders.RemoveParticipationAsync(
            riderId,
            tournamentId,
            rowVersion,
            ct);
    }

    #endregion
}
