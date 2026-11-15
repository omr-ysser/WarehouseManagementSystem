using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public sealed class Tournament
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string? City { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<RiderTournament> Riders { get; set; }
        = new List<RiderTournament>();
}
