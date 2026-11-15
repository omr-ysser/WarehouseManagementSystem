namespace Warehouse.Domain.Entities;

public enum ParticipationStatus
{
    Preparing = 1,
    InTransit = 2,
    Active = 3,
    Completed = 4,
    Canceled = 5
}

public sealed class RiderTournament
{
    public int RiderId { get; set; }

    public Rider Rider { get; set; } = null!;

    public int TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;

    // Availability for this tournament only.
    public bool IsAvailable { get; set; } = true;

    // Progress is separate from availability.
    public ParticipationStatus Status { get; set; }
        = ParticipationStatus.Preparing;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<RiderTournamentHorse> Horses { get; set; }
        = new List<RiderTournamentHorse>();
}

public sealed class RiderTournamentHorse
{
    public int Id { get; set; }

    public int RiderId { get; set; }

    public int TournamentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public RiderTournament Participation { get; set; } = null!;
}