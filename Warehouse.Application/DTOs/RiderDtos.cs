using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.DTOs;

#region Shared Editable Profile

public class RiderProfileRequest
{
    [Required(ErrorMessage = "Please enter the full name.")]
    [StringLength(
        25,
        MinimumLength = 3,
        ErrorMessage = "Full name must be between 3 and 25 characters.")]
    [RegularExpression(
        @"^[\p{L}\p{M}]+(?: [\p{L}\p{M}]+)*$",
        ErrorMessage = "Full name can contain letters and spaces only.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select the nationality.")]
    [CountryCode]
    public string Nationality { get; set; } = string.Empty;
    [Required(ErrorMessage = "Please select the city.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid city.")]
    public int? CityId { get; set; }

    // Nullable is necessary:
    // missing Handicap must not become the valid value 0.
    [Required(ErrorMessage = "Please select the handicap.")]
    [Range(
        -2,
        10,
        ErrorMessage = "Handicap number must be between -2 and 10.")]
    public int? HandicapLevel { get; set; }

    [StringLength(
        25,
        ErrorMessage = "Polo Team must be 25 characters or fewer.")]
    public string? PoloTeam { get; set; }

    // Temporary: accepts a URL until the photo-upload phase.
    [Url(ErrorMessage = "Please enter a valid photo URL.")]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}

#endregion

#region Create Rider

public sealed class RiderRequest : RiderProfileRequest
{
    [Required(ErrorMessage = "Please enter the email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    // Complete international number, for example +201001234567.
    // The frontend combines the selected country code with local digits.
    [RegularExpression(
        @"^\+[1-9][0-9]{7,14}$",
        ErrorMessage = "Please enter a valid international phone number.")]
    public string? PhoneNumber { get; set; }
}

#endregion

#region Update Rider

// Reject unknown fields such as Email and PhoneNumber,
// instead of silently ignoring attempted changes.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateRiderProfileRequest : RiderProfileRequest
{
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateRiderRequest
{
    [Required]
    public UpdateRiderProfileRequest Profile { get; set; } = new();

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

#endregion

#region Tournament Requests

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

#endregion

#region Availability

public sealed class AvailabilityRequest
{
    public bool IsAvailable { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

#endregion

#region Participation Requests

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

#endregion

#region Responses

public sealed record RiderResponse(
    int Id,
    string FullName,
    string Nationality,
    int? CountryId,
    string? CountryCode,
    string? CountryName,
    int? CityId,
    string? CityName,
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

#endregion