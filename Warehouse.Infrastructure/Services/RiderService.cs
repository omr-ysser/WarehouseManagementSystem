using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Services;

public sealed class RiderService : IRiderService
{
    private readonly ApplicationDbContext _db;

    public RiderService(ApplicationDbContext db)
    {
        _db = db;
    }

    #region Rider Queries

    public async Task<PageResponse<RiderResponse>> GetRidersAsync(
        string? search,
        bool? available,
        int page,
        int pageSize,
        CancellationToken ct,
        string[]? nationalities = null,
        int? handicapMin = null,
        int? handicapMax = null)
    {
        ValidatePagination(page, pageSize);
        ValidateHandicapRange(handicapMin, handicapMax);

        var query = _db.Riders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(rider =>
                rider.FullName.Contains(term) ||
                rider.Email.Contains(term) ||
                (
                    rider.PoloTeam != null &&
                    rider.PoloTeam.Contains(term)
                ) ||
                (
                    rider.PhoneNumber != null &&
                    rider.PhoneNumber.Contains(term)
                ));
        }

        if (available.HasValue)
        {
            query = query.Where(
                rider => rider.IsAvailable == available.Value);
        }

        if (nationalities is { Length: > 0 })
        {
            if (nationalities.Any(
                nationality => !CustomerCountries.IsValid(nationality)))
            {
                throw BadRequest("Please select a valid nationality.");
            }

            var countryCodes = nationalities
                .Select(nationality =>
                    nationality.Trim().ToUpperInvariant())
                .Distinct()
                .ToArray();

            query = query.Where(
                rider => countryCodes.Contains(rider.Nationality));
        }

        if (handicapMin.HasValue)
        {
            query = query.Where(
                rider => rider.HandicapLevel >= handicapMin.Value);
        }

        if (handicapMax.HasValue)
        {
            query = query.Where(
                rider => rider.HandicapLevel <= handicapMax.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(rider => rider.RegisteredAtUtc)
            .ThenByDescending(rider => rider.Id)
            .Skip(GetSkip(page, pageSize))
            .Take(pageSize)
            .Select(rider => new
            {
                Rider = rider,
                TournamentCount = rider.Tournaments.Count
            })
            .ToListAsync(ct);

        var items = rows
            .Select(row => MapRider(
                row.Rider,
                row.TournamentCount))
            .ToArray();

        return new PageResponse<RiderResponse>(
            items,
            totalCount,
            page,
            pageSize);
    }

    public async Task<RiderResponse> GetRiderAsync(
        int id,
        CancellationToken ct)
    {
        var rider = await _db.Riders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound("Rider");

        var tournamentCount = await _db.RiderTournaments
            .CountAsync(x => x.RiderId == id, ct);

        return MapRider(rider, tournamentCount);
    }

    #endregion

    #region Rider Commands

    public async Task<RiderResponse> CreateRiderAsync(
        RiderRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);

        // Validate the selected city against the database.
        await EnsureCityExistsAsync(request.CityId, ct);

        var email = request.Email.Trim();

        var rider = new Rider
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PhoneNumber = TrimToNull(request.PhoneNumber),
            IsAvailable = true,
            RegisteredAtUtc = DateTime.UtcNow
        };

        ApplyRiderProfile(rider, request);

        await EnsureUniqueEmailAsync(
            rider.NormalizedEmail,
            null,
            ct);

        _db.Riders.Add(rider);

        await SaveChangesAsync(ct);

        return MapRider(rider, 0);
    }

    public async Task UpdateRiderAsync(
        int id,
        UpdateRiderRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);
        ValidateDto(request.Profile);

        var rider = await RequireRiderAsync(id, ct);

        EnsureRowVersion(
            rider,
            rider.RowVersion,
            request.RowVersion);

        // Update requests contain the profile inside Profile.
        await EnsureCityExistsAsync(request.Profile.CityId, ct);

        // Email and phone are absent from the update DTO.
        ApplyRiderProfile(rider, request.Profile);

        await SaveChangesAsync(ct);
    }

    public async Task DeleteRiderAsync(
        int id,
        string rowVersion,
        CancellationToken ct)
    {
        var rider = await RequireRiderAsync(id, ct);

        EnsureRowVersion(
            rider,
            rider.RowVersion,
            rowVersion);

        var hasParticipations = await _db.RiderTournaments
            .AnyAsync(x => x.RiderId == id, ct);

        if (hasParticipations)
        {
            throw Conflict(
                "This rider has tournament history. Disable the rider instead.");
        }

        _db.Riders.Remove(rider);

        await SaveChangesAsync(ct);
    }

    public async Task SetRiderAvailabilityAsync(
        int id,
        AvailabilityRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);

        var rider = await RequireRiderAsync(id, ct);

        EnsureRowVersion(
            rider,
            rider.RowVersion,
            request.RowVersion);

        rider.IsAvailable = request.IsAvailable;

        await SaveChangesAsync(ct);
    }

    #endregion

    #region Tournament Queries

    public async Task<PageResponse<TournamentResponse>> GetTournamentsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        ValidatePagination(page, pageSize);

        var query = _db.Tournaments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                x.Country.Contains(term) ||
                (
                    x.City != null &&
                    x.City.Contains(term)
                ));
        }

        var totalCount = await query.CountAsync(ct);

        var tournaments = await query
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.Id)
            .Skip(GetSkip(page, pageSize))
            .Take(pageSize)
            .ToListAsync(ct);

        return new PageResponse<TournamentResponse>(
            tournaments.Select(MapTournament).ToArray(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<TournamentResponse> GetTournamentAsync(
        int id,
        CancellationToken ct)
    {
        var tournament = await _db.Tournaments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound("Tournament");

        return MapTournament(tournament);
    }

    #endregion

    #region Tournament Commands

    public async Task<TournamentResponse> CreateTournamentAsync(
        TournamentRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);
        ValidateTournamentDates(request);

        var tournament = new Tournament();

        ApplyTournamentDetails(tournament, request);

        _db.Tournaments.Add(tournament);

        await SaveChangesAsync(ct);

        return MapTournament(tournament);
    }

    public async Task UpdateTournamentAsync(
        int id,
        UpdateTournamentRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);
        ValidateDto(request.Tournament);
        ValidateTournamentDates(request.Tournament);

        var tournament = await RequireTournamentAsync(id, ct);

        EnsureRowVersion(
            tournament,
            tournament.RowVersion,
            request.RowVersion);

        ApplyTournamentDetails(
            tournament,
            request.Tournament);

        await SaveChangesAsync(ct);
    }

    public async Task DeleteTournamentAsync(
        int id,
        string rowVersion,
        CancellationToken ct)
    {
        var tournament = await RequireTournamentAsync(id, ct);

        EnsureRowVersion(
            tournament,
            tournament.RowVersion,
            rowVersion);

        var hasParticipations = await _db.RiderTournaments
            .AnyAsync(x => x.TournamentId == id, ct);

        if (hasParticipations)
        {
            throw Conflict(
                "This tournament has participation history and cannot be deleted.");
        }

        _db.Tournaments.Remove(tournament);

        await SaveChangesAsync(ct);
    }

    #endregion

    #region Participation Queries

    public async Task<PageResponse<ParticipationResponse>>
        GetParticipationsAsync(
            int riderId,
            string? search,
            ParticipationStatus? status,
            int page,
            int pageSize,
            CancellationToken ct)
    {
        ValidatePagination(page, pageSize);

        if (status.HasValue)
        {
            ValidateParticipationStatus(status.Value);
        }

        var riderExists = await _db.Riders
            .AnyAsync(x => x.Id == riderId, ct);

        if (!riderExists)
        {
            throw NotFound("Rider");
        }

        var query = _db.RiderTournaments
            .AsNoTracking()
            .Where(x => x.RiderId == riderId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            // Tournament search starts at 3 characters.
            if (term.Length >= 3)
            {
                query = query.Where(x =>
                    x.Tournament.Name.Contains(term) ||
                    x.Tournament.Country.Contains(term));
            }
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var participations = await query
            .Include(x => x.Rider)
            .Include(x => x.Tournament)
            .Include(x => x.Horses)
            .OrderByDescending(x => x.Tournament.StartDate)
            .ThenBy(x => x.TournamentId)
            .Skip(GetSkip(page, pageSize))
            .Take(pageSize)
            .ToListAsync(ct);

        return new PageResponse<ParticipationResponse>(
            participations.Select(MapParticipation).ToArray(),
            totalCount,
            page,
            pageSize);
    }

    #endregion

    #region Participation Commands

    public async Task<ParticipationResponse> AssignAsync(
        int riderId,
        int tournamentId,
        ParticipationRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);

        var horseNames = ValidateHorseNames(request.Horses);

        await using var transaction = await _db.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        var rider = await RequireRiderAsync(riderId, ct);

        if (!rider.IsAvailable)
        {
            throw Conflict(
                "A disabled rider cannot receive new tournament assignments.");
        }

        var tournament = await RequireTournamentAsync(
            tournamentId,
            ct);

        var alreadyAssigned = await _db.RiderTournaments.AnyAsync(
            x => x.RiderId == riderId &&
                 x.TournamentId == tournamentId,
            ct);

        if (alreadyAssigned)
        {
            throw Conflict(
                "This rider is already assigned to the tournament.");
        }

        var participation = new RiderTournament
        {
            RiderId = riderId,
            Rider = rider,
            TournamentId = tournamentId,
            Tournament = tournament,
            IsAvailable = true,
            Status = ParticipationStatus.Preparing,

            Horses = horseNames
                .Select(name => new RiderTournamentHorse
                {
                    RiderId = riderId,
                    TournamentId = tournamentId,
                    Name = name
                })
                .ToList()
        };

        _db.RiderTournaments.Add(participation);

        await SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return MapParticipation(participation);
    }

    public async Task UpdateHorsesAsync(
        int riderId,
        int tournamentId,
        UpdateParticipationRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);

        var horseNames = ValidateHorseNames(request.Horses);

        var participation = await RequireParticipationAsync(
            riderId,
            tournamentId,
            ct);

        EnsureRowVersion(
            participation,
            participation.RowVersion,
            request.RowVersion);

        EnsureParticipationEditable(participation);

        var previousHorses = participation.Horses.ToArray();

        _db.RiderTournamentHorses.RemoveRange(previousHorses);

        participation.Horses.Clear();

        foreach (var name in horseNames)
        {
            participation.Horses.Add(new RiderTournamentHorse
            {
                RiderId = riderId,
                TournamentId = tournamentId,
                Name = name
            });
        }

        // Updating children must also advance the parent's RowVersion.
        _db.Entry(participation)
            .Property(x => x.IsAvailable)
            .IsModified = true;

        await SaveChangesAsync(ct);
    }

    public async Task SetParticipationAvailabilityAsync(
        int riderId,
        int tournamentId,
        AvailabilityRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);

        await using var transaction = await _db.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        var participation = await RequireParticipationAsync(
            riderId,
            tournamentId,
            ct);

        EnsureRowVersion(
            participation,
            participation.RowVersion,
            request.RowVersion);

        EnsureParticipationEditable(participation);

        if (request.IsAvailable &&
            !participation.Rider.IsAvailable)
        {
            throw Conflict(
                "Enable the rider profile before enabling this participation.");
        }

        participation.IsAvailable = request.IsAvailable;

        await SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task SetParticipationStatusAsync(
        int riderId,
        int tournamentId,
        ParticipationStatusRequest request,
        CancellationToken ct)
    {
        ValidateDto(request);
        ValidateParticipationStatus(request.Status);

        await using var transaction = await _db.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        var participation = await RequireParticipationAsync(
            riderId,
            tournamentId,
            ct);

        EnsureRowVersion(
            participation,
            participation.RowVersion,
            request.RowVersion);

        if (participation.Status == request.Status)
        {
            await transaction.CommitAsync(ct);
            return;
        }

        EnsureParticipationEditable(participation);

        EnsureValidStatusTransition(
            participation.Status,
            request.Status);

        if (request.Status != ParticipationStatus.Canceled)
        {
            EnsureEffectiveAvailability(participation);
        }

        participation.Status = request.Status;

        await SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task RemoveParticipationAsync(
        int riderId,
        int tournamentId,
        string rowVersion,
        CancellationToken ct)
    {
        var participation = await RequireParticipationAsync(
            riderId,
            tournamentId,
            ct);

        EnsureRowVersion(
            participation,
            participation.RowVersion,
            rowVersion);

        if (participation.Status != ParticipationStatus.Preparing)
        {
            throw Conflict(
                "Only preparing participations can be removed. " +
                "Cancel an ongoing participation instead.");
        }

        _db.RiderTournaments.Remove(participation);

        await SaveChangesAsync(ct);
    }

    #endregion

    #region Entity Loading

    private async Task<Rider> RequireRiderAsync(
        int id,
        CancellationToken ct)
    {
        return await _db.Riders
            .SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound("Rider");
    }

    private async Task<Tournament> RequireTournamentAsync(
        int id,
        CancellationToken ct)
    {
        return await _db.Tournaments
            .SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw NotFound("Tournament");
    }

    private async Task<RiderTournament> RequireParticipationAsync(
        int riderId,
        int tournamentId,
        CancellationToken ct)
    {
        return await _db.RiderTournaments
            .Include(x => x.Rider)
            .Include(x => x.Tournament)
            .Include(x => x.Horses)
            .SingleOrDefaultAsync(
                x => x.RiderId == riderId &&
                     x.TournamentId == tournamentId,
                ct)
            ?? throw NotFound("Rider tournament participation");
    }

    #endregion

    #region Validation

    private async Task EnsureCityExistsAsync(
        int? cityId,
        CancellationToken ct)
    {
        if (cityId is not int selectedCityId || selectedCityId <= 0)
        {
            throw BadRequest("Please select a valid city.");
        }

        var exists = await _db.Cities
            .AnyAsync(city => city.Id == selectedCityId, ct);

        if (!exists)
        {
            throw BadRequest("The selected city does not exist.");
        }
    }

    private static void ValidateDto(object? request)
    {
        if (request is null)
        {
            throw BadRequest("Request body is required.");
        }

        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        if (!valid)
        {
            var message = string.Join(
                " ",
                results
                    .Select(x => x.ErrorMessage)
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            throw BadRequest(message);
        }
    }

    private static void ValidatePagination(
        int page,
        int pageSize)
    {
        if (page < 1)
        {
            throw BadRequest("Page must be at least 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw BadRequest(
                "PageSize must be between 1 and 100.");
        }

        if ((long)(page - 1) * pageSize > int.MaxValue)
        {
            throw BadRequest("Requested page is too large.");
        }
    }

    private static int GetSkip(
        int page,
        int pageSize)
    {
        return checked((page - 1) * pageSize);
    }

    private static void ValidateHandicapRange(
        int? handicapMin,
        int? handicapMax)
    {
        if (handicapMin is < -2 or > 10 ||
            handicapMax is < -2 or > 10)
        {
            throw BadRequest(
                "Handicap range must be between -2 and 10.");
        }

        if (handicapMin.HasValue &&
            handicapMax.HasValue &&
            handicapMin.Value > handicapMax.Value)
        {
            throw BadRequest(
                "Minimum handicap cannot exceed maximum handicap.");
        }
    }

    private async Task EnsureUniqueEmailAsync(
        string normalizedEmail,
        int? excludingRiderId,
        CancellationToken ct)
    {
        var exists = await _db.Riders.AnyAsync(
            x => x.NormalizedEmail == normalizedEmail &&
                 (
                     !excludingRiderId.HasValue ||
                     x.Id != excludingRiderId.Value
                 ),
            ct);

        if (exists)
        {
            throw Conflict(
                "An account with this email already exists.");
        }
    }

    private static void ValidateTournamentDates(
        TournamentRequest request)
    {
        if (request.StartDate == default ||
            request.EndDate == default)
        {
            throw BadRequest(
                "Tournament start and end dates are required.");
        }

        if (request.EndDate < request.StartDate)
        {
            throw BadRequest(
                "Tournament end date cannot precede its start date.");
        }
    }

    private static string[] ValidateHorseNames(string[]? horses)
    {
        if (horses is null || horses.Length is < 1 or > 50)
        {
            throw BadRequest(
                "Provide between 1 and 50 horses.");
        }

        if (horses.Any(name =>
                string.IsNullOrWhiteSpace(name) ||
                name.Trim().Length > 150))
        {
            throw BadRequest(
                "Each horse name must contain 1–150 characters.");
        }

        var names = horses
            .Select(name => name.Trim())
            .ToArray();

        if (names
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != names.Length)
        {
            throw BadRequest(
                "Duplicate horse names are not allowed.");
        }

        return names;
    }

    private static void ValidateParticipationStatus(
        ParticipationStatus status)
    {
        if (!Enum.IsDefined(typeof(ParticipationStatus), status))
        {
            throw BadRequest("Invalid participation status.");
        }
    }

    private static void EnsureParticipationEditable(
        RiderTournament participation)
    {
        if (participation.Status is
            ParticipationStatus.Completed or
            ParticipationStatus.Canceled)
        {
            throw Conflict(
                "Completed and canceled participations are read-only.");
        }
    }

    private static void EnsureEffectiveAvailability(
        RiderTournament participation)
    {
        if (!participation.Rider.IsAvailable)
        {
            throw Conflict("The rider profile is disabled.");
        }

        if (!participation.IsAvailable)
        {
            throw Conflict(
                "The rider is disabled for this tournament.");
        }
    }

    private static void EnsureValidStatusTransition(
        ParticipationStatus current,
        ParticipationStatus next)
    {
        var allowed = (current, next) switch
        {
            (
                ParticipationStatus.Preparing,
                ParticipationStatus.InTransit
            ) => true,

            (
                ParticipationStatus.InTransit,
                ParticipationStatus.Active
            ) => true,

            (
                ParticipationStatus.Active,
                ParticipationStatus.Completed
            ) => true,

            (
                ParticipationStatus.Preparing,
                ParticipationStatus.Canceled
            ) => true,

            (
                ParticipationStatus.InTransit,
                ParticipationStatus.Canceled
            ) => true,

            (
                ParticipationStatus.Active,
                ParticipationStatus.Canceled
            ) => true,

            _ => false
        };

        if (!allowed)
        {
            throw Conflict(
                $"Cannot change participation status from {current} to {next}.");
        }
    }

    #endregion

    #region Concurrency and Persistence

    private void EnsureRowVersion<TEntity>(
        TEntity entity,
        byte[] currentVersion,
        string? suppliedVersion)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(suppliedVersion))
        {
            throw BadRequest("RowVersion is required.");
        }

        byte[] expectedVersion;

        try
        {
            expectedVersion = Convert.FromBase64String(
                suppliedVersion);
        }
        catch (FormatException)
        {
            throw BadRequest(
                "RowVersion must be a valid Base64 string.");
        }

        if (expectedVersion.Length != 8)
        {
            throw BadRequest("Invalid SQL Server RowVersion.");
        }

        if (!currentVersion.SequenceEqual(expectedVersion))
        {
            throw Conflict(
                "This record has changed. Reload it before trying again.");
        }

        _db.Entry(entity)
            .Property("RowVersion")
            .OriginalValue = expectedVersion;
    }

    private async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw Conflict(
                "This record was changed or deleted by another user. " +
                "Reload it before trying again.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException
            {
                Number: 2601 or 2627
            })
        {
            throw Conflict(
                "A record with the same unique value already exists.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException
            {
                Number: 547
            })
        {
            throw Conflict(
                "Related records prevent this operation.");
        }
    }

    #endregion

    #region Mapping

    private static void ApplyRiderProfile(
        Rider rider,
        RiderProfileRequest request)
    {
        if (request.HandicapLevel is not int handicap)
        {
            throw BadRequest("Please select the handicap.");
        }

        if (handicap is < -2 or > 10)
        {
            throw BadRequest(
                "Handicap number must be between -2 and 10.");
        }

        if (request.CityId is not int cityId || cityId <= 0)
        {
            throw BadRequest("Please select a valid city.");
        }

        rider.FullName = request.FullName.Trim();

        rider.Nationality = request.Nationality
            .Trim()
            .ToUpperInvariant();

        // Country is determined through City.CountryId.
        rider.CityId = cityId;

        rider.HandicapLevel = handicap;
        rider.PoloTeam = TrimToNull(request.PoloTeam);
        rider.ImageUrl = TrimToNull(request.ImageUrl);
    }

    private static void ApplyTournamentDetails(
        Tournament tournament,
        TournamentRequest request)
    {
        tournament.Name = request.Name.Trim();
        tournament.Country = request.Country.Trim();
        tournament.City = TrimToNull(request.City);
        tournament.StartDate = request.StartDate;
        tournament.EndDate = request.EndDate;
    }

    private static RiderResponse MapRider(
    Rider rider,
    int tournamentCount)
    {
        return new RiderResponse(
            rider.Id,
            rider.FullName,
            rider.Nationality,
            rider.City?.CountryId,
            rider.City?.Country.Code,
            rider.City?.Country.Name,
            rider.CityId,
            rider.City?.Name,
            rider.Email,
            rider.PhoneNumber,
            rider.HandicapLevel,
            rider.PoloTeam,
            rider.ImageUrl,
            rider.IsAvailable,
            rider.RegisteredAtUtc,
            tournamentCount,
            Convert.ToBase64String(rider.RowVersion));
    }

    private static TournamentResponse MapTournament(
        Tournament tournament)
    {
        return new TournamentResponse(
            tournament.Id,
            tournament.Name,
            tournament.Country,
            tournament.City,
            tournament.StartDate,
            tournament.EndDate,
            Convert.ToBase64String(tournament.RowVersion));
    }

    private static ParticipationResponse MapParticipation(
        RiderTournament participation)
    {
        return new ParticipationResponse(
            participation.RiderId,
            participation.TournamentId,
            participation.Tournament.Name,
            participation.Tournament.Country,
            participation.Tournament.City,
            participation.Tournament.StartDate,
            participation.Tournament.EndDate,
            participation.IsAvailable,
            participation.Rider.IsAvailable &&
                participation.IsAvailable,
            participation.Status,
            participation.Horses
                .OrderBy(horse => horse.Name)
                .Select(horse => horse.Name)
                .ToArray(),
            Convert.ToBase64String(participation.RowVersion));
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    #endregion

    #region Exceptions

    private static AppException BadRequest(string message)
    {
        return new AppException(400, message);
    }

    private static AppException NotFound(string resource)
    {
        return new AppException(
            404,
            $"{resource} not found.");
    }

    private static AppException Conflict(string message)
    {
        return new AppException(409, message);
    }

    #endregion
}