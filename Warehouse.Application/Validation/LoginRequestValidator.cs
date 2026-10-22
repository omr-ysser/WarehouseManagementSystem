using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using Warehouse.Application.DTOs;

namespace Warehouse.Application.Validation;

public sealed class LoginRequestValidator
    : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Email is required.")
            .MaximumLength(256)
                .WithMessage("Email must not exceed 256 characters.")
            .EmailAddress()
                .WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Password is required.")
            .MaximumLength(128)
                .WithMessage("Password must not exceed 128 characters.");
    }
}
