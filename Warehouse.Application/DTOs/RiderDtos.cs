using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.DTOs;

public sealed class RiderRequest
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Nationality { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    [Range(-2, 10)]
    public int HandicapLevel { get; set; }

    [StringLength(150)]
    public string? PoloTeam { get; set; }

    [Url, StringLength(2048)]
    public string? ImageUrl { get; set; }

    // Required for updates; omitted when creating.
    public Guid? Version { get; set; }
}

// Use SQL rowversion as a Base64 string instead of Guid.
// This separate request carries the concurrency token for PUT.
public sealed class UpdateRiderRequest
{
    [Required]
    public RiderRequest Profile { get; set; } = new();

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class TournamentRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [StringLength(100)]
    public string? City { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}

public sealed class UpdateTournamentRequest
{
    [Required]
    public TournamentRequest Tournament { get; set; } = new();

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class AvailabilityRequest
{
    public bool IsAvailable { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ParticipationRequest
{
    [Required, MinLength(1), MaxLength(50)]
    public string[] Horses { get; set; } = [];
}

public sealed class UpdateParticipationRequest
{
    [Required, MinLength(1), MaxLength(50)]
    public string[] Horses { get; set; } = [];

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ParticipationStatusRequest
{
    [EnumDataType(typeof(ParticipationStatus))]
    public ParticipationStatus Status { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed record RiderResponse(
    int Id,
    string FullName,
    string Nationality,
    string Email,
    string? PhoneNumber,
    int HandicapLevel,
    string? PoloTeam,
    string? ImageUrl,
    bool IsAvailable,
    DateTime RegisteredAtUtc,
    int TournamentCount,
    string RowVersion);

public sealed record TournamentResponse(
    int Id,
    string Name,
    string Country,
    string? City,
    DateOnly StartDate,
    DateOnly EndDate,
    string RowVersion);

public sealed record ParticipationResponse(
    int RiderId,
    int TournamentId,
    string TournamentName,
    string Country,
    string? City,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsAvailable,
    bool EffectiveAvailability,
    ParticipationStatus Status,
    IReadOnlyList<string> Horses,
    string RowVersion);

public sealed record PageResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
