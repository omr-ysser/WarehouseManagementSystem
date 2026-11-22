using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public sealed class Supplier
{
    public int Id { get; set; }

    #region Simple Attributes

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public string CreatedByAccountId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    #endregion


    // Required primary contact.
    public SupplierContact PrimaryContact { get; set; } = new();

    // Optional additional contacts.
    public SupplierContact? AdditionalContact1 { get; set; }

    public SupplierContact? AdditionalContact2 { get; set; }

    public SupplierContact? AdditionalContact3 { get; set; }
    public List<SupplierTruck> Trucks { get; set; } = [];
    public string? Note { get; set; }

    public string? NoteCreatedByAccountId { get; set; }

    public DateTime? NoteCreatedAtUtc { get; set; }


    #region Documents Relationship

    public ICollection<SupplierDocument> Documents { get; set; }
        = new List<SupplierDocument>();

    #endregion
}
