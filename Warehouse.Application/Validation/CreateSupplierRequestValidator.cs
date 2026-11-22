using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Validation;

public sealed class CreateSupplierRequestValidator
    : AbstractValidator<CreateSupplierRequest>
{
    private static readonly Regex TruckPattern = new(
        @"\A[\p{L}0-9]{7,20}\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public CreateSupplierRequestValidator()
    {
        #region Supplier Name

        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please enter the supplier name.")
            .MaximumLength(150)
            .WithMessage(
                "Supplier name must be 150 characters or fewer.");

        #endregion

        #region Optional Email

        When(
            request => !string.IsNullOrWhiteSpace(request.Email),
            () =>
            {
                RuleFor(request => request.Email)
                    .Cascade(CascadeMode.Stop)
                    .MaximumLength(256)
                    .WithMessage(
                        "Email must be 256 characters or fewer.")
                    .EmailAddress()
                    .WithMessage(
                        "Please enter a valid email address.");
            });

        #endregion

        #region Required Primary Contact

        RuleFor(request => request.PrimaryContact)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Please enter the primary contact.")
            .SetValidator(new SupplierContactRequestValidator());

        #endregion

        #region Optional Additional Contacts

        RuleFor(request => request.AdditionalContact1)
            .SetValidator(new SupplierContactRequestValidator());

        RuleFor(request => request.AdditionalContact2)
            .SetValidator(new SupplierContactRequestValidator());

        RuleFor(request => request.AdditionalContact3)
            .SetValidator(new SupplierContactRequestValidator());

        #endregion

        #region Required Documents

        RuleFor(request => request.TaxNumberDocumentId)
            .GreaterThan(0)
            .WithMessage(
                "Please upload the tax number document.");

        RuleFor(request => request.CommercialRegistrationDocumentId)
            .GreaterThan(0)
            .WithMessage(
                "Please upload the commercial registration document.");

        RuleFor(request => request.CommercialRegistrationDocumentId)
            .NotEqual(request => request.TaxNumberDocumentId)
            .WithMessage(
                "Tax and commercial registration documents must be different.")
            .When(request =>
                request.TaxNumberDocumentId > 0 &&
                request.CommercialRegistrationDocumentId > 0);

        #endregion

        #region Required Trucks

        RuleFor(request => request.Trucks)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please add at least one truck.")
            .Must(HaveUniqueTrucks)
            .WithMessage(
                "The same truck cannot be added more than once.");

        RuleForEach(request => request.Trucks)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Please enter the truck value.")
            .Must(BeValidTruck)
            .WithMessage(
                "Truck value must contain 7–20 letters or digits.");

        #endregion
    }

    #region Helper Methods

    private static bool BeValidTruck(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return TruckPattern.IsMatch(value.Trim());
    }

    private static bool HaveUniqueTrucks(List<string>? trucks)
    {
        if (trucks is null)
        {
            // NotEmpty handles a missing collection.
            return true;
        }

        var values = trucks
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();

        return values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == values.Length;
    }

    #endregion
}