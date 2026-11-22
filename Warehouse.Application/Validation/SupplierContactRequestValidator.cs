using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Validation;

public sealed class SupplierContactRequestValidator
    : AbstractValidator<SupplierContactRequest>
{
    public SupplierContactRequestValidator()
    {
        #region Person

        RuleFor(contact => contact.Person)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please enter the contact name.")
            .MaximumLength(150)
            .WithMessage(
                "Contact name must be 150 characters or fewer.");

        #endregion

        #region Phone Number

        RuleFor(contact => contact.PhoneNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please enter the phone number.")
            .MaximumLength(30)
            .WithMessage(
                "Phone number must be 30 digits or fewer.")
            .Matches(@"\A[0-9]+\z")
            .WithMessage(
                "Phone number must contain digits only.");

        #endregion

        #region Job Title

        RuleFor(contact => contact.JobTitle)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please enter the job title.")
            .MaximumLength(100)
            .WithMessage(
                "Job title must be 100 characters or fewer.");

        #endregion
    }
}
