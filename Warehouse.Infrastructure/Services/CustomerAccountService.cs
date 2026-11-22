using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Identity;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Services;

public sealed class CustomerAccountService
    : ICustomerAccountService
{
    public const string DisabledMessage =
        "Your account has been disabled. Please contact support.";

    private const string CustomerRoleName = "Customer";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;

    public CustomerAccountService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }

    public async Task<string> CreateAccountAsync(
        Rider rider,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Customer account creation requires an active transaction.");
        }

        var existingAccount = await _users.FindByEmailAsync(
            rider.Email);

        if (existingAccount is not null)
        {
            throw new AppException(
                409,
                "An account with this email already exists.");
        }

        var customerRole = await _roles.FindByNameAsync(
            CustomerRoleName);

        if (customerRole is null)
        {
            customerRole = new ApplicationRole
            {
                Name = CustomerRoleName
            };

            EnsureSuccess(
                await _roles.CreateAsync(customerRole));
        }

        var user = new ApplicationUser
        {
            UserName = rider.Email,
            Email = rider.Email,
            FullName = rider.FullName,
            PhoneNumber = rider.PhoneNumber,

            // The customer has not verified ownership yet.
            EmailConfirmed = false,
            PhoneNumberConfirmed = false
        };

        // No password is created by the admin.
        EnsureSuccess(
            await _users.CreateAsync(user));

        // Customer role has no admin permissions by default.
        EnsureSuccess(
            await _users.AddToRoleAsync(
                user,
                CustomerRoleName));

        return user.Id;
    }

    public async Task SynchronizeNameAsync(
        Rider rider,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (rider.AccountId is null)
        {
            return;
        }

        var user = await _users.FindByIdAsync(
            rider.AccountId);

        if (user is null)
        {
            throw new AppException(
                409,
                "The customer's linked account was not found.");
        }

        user.FullName = rider.FullName;

        EnsureSuccess(
            await _users.UpdateAsync(user));
    }

    public Task<bool> IsDisabledAsync(
        string accountId,
        CancellationToken ct)
    {
        return _db.Riders
            .AsNoTracking()
            .AnyAsync(
                rider =>
                    rider.AccountId == accountId &&
                    !rider.IsAvailable,
                ct);
    }

    public Task<int?> GetRiderIdAsync(
        string accountId,
        CancellationToken ct)
    {
        return _db.Riders
            .AsNoTracking()
            .Where(rider => rider.AccountId == accountId)
            .Select(rider => (int?)rider.Id)
            .SingleOrDefaultAsync(ct);
    }

    public async Task ProvisionExistingRiderAsync(
        int riderId,
        CancellationToken ct)
    {
        await using var transaction = await _db.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        var rider = await _db.Riders
            .SingleOrDefaultAsync(
                item => item.Id == riderId,
                ct)
            ?? throw new AppException(
                404,
                "Rider not found.");

        if (rider.AccountId is not null)
        {
            throw new AppException(
                409,
                "This customer already has a linked account.");
        }

        if (!rider.IsAvailable)
        {
            throw new AppException(
                409,
                "Activate the customer before creating an account.");
        }

        rider.AccountId = await CreateAccountAsync(
            rider,
            ct);

        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
    }

    private static void EnsureSuccess(
        IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors.ToArray();

        var isConflict = errors.Any(error =>
            error.Code.StartsWith(
                "Duplicate",
                StringComparison.Ordinal) ||
            error.Code == "ConcurrencyFailure");

        throw new AppException(
            isConflict ? 409 : 400,
            string.Join(
                " ",
                errors.Select(error => error.Description)));
    }
}
