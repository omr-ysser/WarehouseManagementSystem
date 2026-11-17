using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace Warehouse.Application.DTOs;

public sealed class SetCustomerPasswordRequest
{
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(4096)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(
        128,
        MinimumLength = 8,
        ErrorMessage = "Password must contain between 8 and 128 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(
        nameof(Password),
        ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
