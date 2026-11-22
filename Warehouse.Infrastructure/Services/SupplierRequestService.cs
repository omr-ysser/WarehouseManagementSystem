using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Services;

public sealed class SupplierRequestService(ApplicationDbContext db)
    : ISupplierRequestService
{
    private const decimal MaxAmount = 9_999_999_999_999_999.99m;
    private const decimal MaxLoadWeight = 999_999_999_999_999.999m;

    public async Task<IReadOnlyList<SupplierRequestResponse>> ListAsync(
        int supplierId,
        SupplierRequestFilter filter,
        CancellationToken ct)
    {
        await EnsureSupplierExistsAsync(supplierId, ct);

        if (filter.DateFrom is { } start &&
            filter.DateTo is { } end &&
            start > end)
        {
            throw new AppException(400, "Date From must be on or before Date To.");
        }

        var requests = ForSupplier(supplierId).AsNoTracking();

        if (filter.FulfillmentSource is { } source)
        {
            if (!Enum.IsDefined(source))
            {
                throw new AppException(400, "Invalid fulfillment source.");
            }

            requests = requests.Where(request => request.FulfillmentSource == source);
        }

        if (filter.DateFrom is { } dateFrom)
        {
            requests = requests.Where(request => request.ToDate >= dateFrom);
        }

        if (filter.DateTo is { } dateTo)
        {
            requests = requests.Where(request => request.FromDate <= dateTo);
        }

        var results = await requests
            .OrderByDescending(request => request.FromDate)
            .ThenByDescending(request => request.Id)
            .ToListAsync(ct);

        return results.Select(Map).ToArray();
    }

    public async Task<SupplierRequestResponse> GetAsync(
        int supplierId,
        int requestId,
        CancellationToken ct)
    {
        var request = await ForSupplier(supplierId)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == requestId, ct);

        return Map(request ?? throw RequestNotFound());
    }

    public async Task<SupplierRequestResponse> CreateAsync(
        int supplierId,
        CreateSupplierRequestInput input,
        CancellationToken ct)
    {
        var orderId = input.OrderId?.Trim();

        if (string.IsNullOrWhiteSpace(orderId) || orderId.Length > 64)
        {
            throw new AppException(
                400,
                "Order ID is required and must be at most 64 characters.");
        }

        if (input.FromDate == default || input.ToDate < input.FromDate)
        {
            throw new AppException(400, "Provide a valid request date range.");
        }

        if (input.LoadWeight <= 0 ||
            input.LoadWeight > MaxLoadWeight ||
            decimal.Round(input.LoadWeight, 3) != input.LoadWeight)
        {
            throw new AppException(
                400,
                "Provide a positive load weight with up to three decimal places.");
        }

        if (!Enum.IsDefined(input.FulfillmentSource))
        {
            throw new AppException(400, "Invalid fulfillment source.");
        }

        await using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var isActive = await db.Suppliers
            .Where(supplier => supplier.Id == supplierId)
            .Select(supplier => (bool?)supplier.IsActive)
            .SingleOrDefaultAsync(ct);

        if (isActive is null)
        {
            throw new AppException(404, "Supplier not found.");
        }

        if (isActive is false)
        {
            throw new AppException(
                409,
                "An inactive supplier cannot be assigned to a new request.");
        }

        var request = new SupplierRequest
        {
            SupplierId = supplierId,
            OrderId = orderId,
            FromDate = input.FromDate,
            ToDate = input.ToDate,
            LoadWeight = input.LoadWeight,
            FulfillmentSource = input.FulfillmentSource
        };

        db.SupplierRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Map(request);
    }

    public async Task<SupplierRequestResponse> UpdateFinanceAsync(
        int supplierId,
        int requestId,
        UpdateSupplierFinanceInput input,
        string actorAccountId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actorAccountId))
        {
            throw new AppException(401, "A valid authenticated user is required.");
        }

        var request = await ForSupplier(supplierId)
            .SingleOrDefaultAsync(item => item.Id == requestId, ct)
            ?? throw RequestNotFound();

        if (input.CollectedAmount is not decimal collectedAmount ||
            input.Payments is not decimal payments ||
            !IsValidAmount(collectedAmount) ||
            !IsValidAmount(payments))
        {
            throw new AppException(
                400,
                "Both amounts must be non-negative, fit within 16 whole digits, and have up to two decimal places.");
        }

        if (request.CollectedAmount == collectedAmount &&
            request.Payments == payments)
        {
            return Map(request);
        }

        var activity = new SupplierRequestFinanceActivity
        {
            SupplierRequestId = request.Id,
            ChangedByAccountId = actorAccountId,
            ChangedAtUtc = DateTime.UtcNow,
            PreviousCollectedAmount = request.CollectedAmount,
            PreviousPayments = request.Payments,
            CollectedAmount = collectedAmount,
            Payments = payments
        };

        request.CollectedAmount = collectedAmount;
        request.Payments = payments;
        db.SupplierRequestFinanceActivities.Add(activity);

        try
        {
            // Save the financial change and its activity entry together.
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(
                409,
                "The request finances changed while saving. Reload the request and try again.");
        }

        return Map(request);
    }

    public async Task<IReadOnlyList<SupplierFinanceActivityResponse>> ListActivityAsync(
        int supplierId,
        int requestId,
        CancellationToken ct)
    {
        var request = ForSupplier(supplierId)
            .Where(item => item.Id == requestId);

        if (!await request.AnyAsync(ct))
        {
            throw RequestNotFound();
        }

        return await db.SupplierRequestFinanceActivities
            .AsNoTracking()
            .Where(activity => request.Any(item => item.Id == activity.SupplierRequestId))
            .OrderByDescending(activity => activity.Id)
            .Select(activity => new SupplierFinanceActivityResponse(
                activity.ChangedByAccountId,
                activity.ChangedAtUtc,
                activity.PreviousCollectedAmount,
                activity.PreviousPayments,
                activity.CollectedAmount,
                activity.Payments))
            .ToArrayAsync(ct);
    }

    private IQueryable<SupplierRequest> ForSupplier(int supplierId) =>
        db.SupplierRequests.Where(request => request.SupplierId == supplierId);

    private async Task EnsureSupplierExistsAsync(int supplierId, CancellationToken ct)
    {
        var exists = await db.Suppliers.AnyAsync(
            supplier => supplier.Id == supplierId,
            ct);

        if (!exists)
        {
            throw new AppException(404, "Supplier not found.");
        }
    }

    private static bool IsValidAmount(decimal amount) =>
        amount >= 0 && amount <= MaxAmount && decimal.Round(amount, 2) == amount;

    private static AppException RequestNotFound() =>
        new(404, "Supplier request not found.");

    private static SupplierRequestResponse Map(SupplierRequest request) =>
        new(
            request.Id,
            request.SupplierId,
            request.OrderId,
            request.FromDate,
            request.ToDate,
            request.LoadWeight,
            request.FulfillmentSource.ToString(),
            request.Status,
            request.CollectedAmount,
            request.Payments,
            request.CollectedAmount - request.Payments);
}