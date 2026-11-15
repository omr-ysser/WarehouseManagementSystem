using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public sealed class Rider
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    // Store the complete international number, e.g. +201000000000.
    public string? PhoneNumber { get; set; }

    // Polo handicap range.
    public int HandicapLevel { get; set; }

    public string? PoloTeam { get; set; }

    // URL of an image already uploaded by your media module.
    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<RiderTournament> Tournaments { get; set; }
        = new List<RiderTournament>();
}
