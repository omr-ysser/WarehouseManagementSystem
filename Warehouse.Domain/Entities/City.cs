using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public sealed class City
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Country Country { get; set; } = null!;

    public ICollection<Rider> Riders { get; set; }
        = new List<Rider>();
}
