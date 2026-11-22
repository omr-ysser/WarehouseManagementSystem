using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Application.DTOs;

public sealed class AddSupplierNoteRequest
{
    public string Content { get; set; } = string.Empty;
}
