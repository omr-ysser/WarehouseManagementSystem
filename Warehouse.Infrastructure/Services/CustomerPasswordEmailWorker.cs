using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Infrastructure.Identity;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Services;

public sealed class CustomerPasswordEmailWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CustomerPasswordOptions _options;
    private readonly ILogger<CustomerPasswordEmailWorker> _logger;

    public CustomerPasswordEmailWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<CustomerPasswordOptions> options,
        ILogger<CustomerPasswordEmailWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.EmailEnabled)
        {
            _logger.LogInformation(
                "Customer password email delivery is disabled");

            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextAsync(
                    stoppingToken);

                if (processed)
                {
                    continue;
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // Do not log SMTP credentials, recipients or password links.
                _logger.LogWarning(
                    "Customer password email processing failed; it will retry");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(15),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<bool> ProcessNextAsync(
        CancellationToken ct)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var users = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        // Lock the selected outbox row while sending.
        // Other workers skip rows already locked by a dispatcher.
        await using var transaction = await db.Database
            .BeginTransactionAsync(
                IsolationLevel.RepeatableRead,
                ct);

        var now = DateTime.UtcNow;

        var email = await db.CustomerPasswordEmails
            .FromSqlInterpolated($"""
                SELECT TOP (1) *
                FROM [CustomerPasswordEmails]
                    WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE [ProcessedAtUtc] IS NULL
                  AND [NextAttemptAtUtc] <= {now}
                ORDER BY [Id]
                """)
            .SingleOrDefaultAsync(ct);

        if (email is null)
        {
            return false;
        }

        var rider = await db.Riders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == email.RiderId &&
                    item.AccountId == email.AccountId,
                ct);

        var user = await users.FindByIdAsync(
            email.AccountId);

        if (rider is null ||
            !rider.IsAvailable ||
            user is null ||
            await users.HasPasswordAsync(user))
        {
            // No invitation is sent for disabled or already activated accounts.
            email.ProcessedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return true;
        }

        email.Attempts++;

        try
        {
            var token = await users.GeneratePasswordResetTokenAsync(
                user);

            var passwordUrl = QueryHelpers.AddQueryString(
                _options.PasswordPageUrl,
                new Dictionary<string, string?>
                {
                    ["userId"] = user.Id,
                    ["token"] = token
                });

            using var client = new SmtpClient(
                _options.Smtp.Host,
                _options.Smtp.Port)
            {
                EnableSsl = _options.Smtp.EnableSsl,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(
                    _options.Smtp.Username))
            {
                client.Credentials = new NetworkCredential(
                    _options.Smtp.Username,
                    _options.Smtp.Password);
            }

            using var message = new MailMessage(
                _options.FromAddress,
                user.Email!)
            {
                Subject = "Create your customer password",
                IsBodyHtml = false,
                Body =
                    $"Hello {rider.FullName},\n\n" +
                    "Your customer account has been created.\n\n" +
                    "Create your password using this link:\n" +
                    $"{passwordUrl}\n\n" +
                    "The link expires after 24 hours and can only be used " +
                    "to create your first password.\n\n" +
                    "If you were not expecting this email, contact support."
            };

            using var sendTimeout =
                CancellationTokenSource.CreateLinkedTokenSource(ct);

            sendTimeout.CancelAfter(
                TimeSpan.FromSeconds(30));

            await client.SendMailAsync(
                message,
                sendTimeout.Token);

            email.SentAtUtc = DateTime.UtcNow;
            email.ProcessedAtUtc = email.SentAtUtc;

            _logger.LogInformation(
                "Customer password email {EmailId} was accepted by SMTP",
                email.Id);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            var delayMinutes = Math.Min(
                60,
                email.Attempts * 2);

            email.NextAttemptAtUtc = DateTime.UtcNow
                .AddMinutes(delayMinutes);

            _logger.LogWarning(
                "Customer password email {EmailId} failed on attempt {Attempt}; it will retry",
                email.Id,
                email.Attempts);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }
}
