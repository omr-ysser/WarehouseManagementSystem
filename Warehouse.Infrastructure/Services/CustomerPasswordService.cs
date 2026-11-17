using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Identity;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Services;

public sealed class CustomerPasswordService
    : ICustomerPasswordService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CustomerPasswordService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task QueueInvitationAsync(
        int riderId,
        CancellationToken ct)
    {
        // Reuse the customer-creation transaction when one exists.
        // Otherwise create a transaction for an explicit resend.
        await using var transaction =
            _db.Database.CurrentTransaction is null
                ? await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    ct)
                : null;

        var rider = await _db.Riders
            .SingleOrDefaultAsync(
                item => item.Id == riderId,
                ct)
            ?? throw new AppException(
                404,
                "Rider not found.");

        if (!rider.IsAvailable)
        {
            throw new AppException(
                409,
                "Activate the customer before sending a password invitation.");
        }

        if (rider.AccountId is null)
        {
            throw new AppException(
                409,
                "Create the customer's linked account first.");
        }

        var user = await _users.FindByIdAsync(
            rider.AccountId);

        if (user is null)
        {
            throw new AppException(
                409,
                "The customer's linked account was not found.");
        }

        if (await _users.HasPasswordAsync(user))
        {
            throw new AppException(
                409,
                "The customer has already created a password.");
        }

        var alreadyQueued = await _db.CustomerPasswordEmails
            .AnyAsync(
                email =>
                    email.AccountId == user.Id &&
                    email.ProcessedAtUtc == null,
                ct);

        if (!alreadyQueued)
        {
            // Invalidates previously issued invitation links.
            EnsureSuccess(
                await _users.UpdateSecurityStampAsync(user));

            _db.CustomerPasswordEmails.Add(
                new CustomerPasswordEmail
                {
                    RiderId = rider.Id,
                    AccountId = user.Id
                });

            await _db.SaveChangesAsync(ct);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }
    }

    public async Task SetPasswordAsync(
        SetCustomerPasswordRequest request,
        CancellationToken ct)
    {
        ValidateRequest(request);

        var user = await _users.FindByIdAsync(
            request.UserId);

        if (user is null)
        {
            throw InvalidLink();
        }

        var rider = await _db.Riders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.AccountId == user.Id,
                ct);

        if (rider is null ||
            await _users.HasPasswordAsync(user))
        {
            throw InvalidLink();
        }

        if (!rider.IsAvailable)
        {
            throw new AppException(
                403,
                CustomerAccountService.DisabledMessage);
        }

        ct.ThrowIfCancellationRequested();

        // Possession of the emailed link verifies the email address.
        // ResetPasswordAsync saves this together with the password change.
        user.EmailConfirmed = true;

        var result = await _users.ResetPasswordAsync(
            user,
            request.Token,
            request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.ToArray();

            if (errors.Any(error =>
                    error.Code == "InvalidToken" ||
                    error.Code == "ConcurrencyFailure"))
            {
                throw InvalidLink();
            }

            throw new AppException(
                400,
                string.Join(
                    " ",
                    errors.Select(error => error.Description)));
        }

        // Identity changes the security stamp after resetting the password.
        // The used link and other older invitation links become invalid.
    }

    private static void ValidateRequest(
        SetCustomerPasswordRequest? request)
    {
        if (request is null)
        {
            throw new AppException(
                400,
                "Request body is required.");
        }

        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        if (!valid)
        {
            throw new AppException(
                400,
                string.Join(
                    " ",
                    results.Select(result => result.ErrorMessage)));
        }
    }

    private static AppException InvalidLink()
    {
        return new AppException(
            400,
            "This password link is invalid, expired or has already been used.");
    }

    private static void EnsureSuccess(
        IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new AppException(
                400,
                string.Join(
                    " ",
                    result.Errors.Select(error => error.Description)));
        }
    }
}
