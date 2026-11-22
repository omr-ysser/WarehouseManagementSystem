using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Validation;

public sealed class AddSupplierNoteRequestValidator
    : AbstractValidator<AddSupplierNoteRequest>
{
    public AddSupplierNoteRequestValidator()
    {
        RuleFor(request => request.Content)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please enter the note.")
            .MaximumLength(5000)
            .WithMessage(
                "Note must be 5000 characters or fewer.");
    }
}
