using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed class UpdateSupplierInput
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool? IsActive { get; set; }

    public SupplierContactRequest PrimaryContact { get; set; } = new();
    public SupplierContactRequest? AdditionalContact1 { get; set; }
    public SupplierContactRequest? AdditionalContact2 { get; set; }
    public SupplierContactRequest? AdditionalContact3 { get; set; }
    public int? TaxNumberDocumentId { get; set; }
    public int? CommercialRegistrationDocumentId { get; set; }

    public List<string> Trucks { get; set; } = [];
}
