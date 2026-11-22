namespace Warehouse.Domain.Entities;

public sealed class Rider
{
    public int Id { get; set; }

    // Nullable to preserve existing riders during migration.
    public string? AccountId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public int HandicapLevel { get; set; }

    public string? PoloTeam { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = [];
    public int? CityId { get; set; }

    public City? City { get; set; }

    public ICollection<RiderTournament> Tournaments { get; set; }
        = new List<RiderTournament>();
}