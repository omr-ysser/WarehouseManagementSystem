using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Domain.Entities;

public sealed class CustomerPasswordEmail
{
    public long Id { get; set; }

    public int RiderId { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;

    public int Attempts { get; set; }

    // Set when the email was sent or the invitation became unnecessary.
    public DateTime? ProcessedAtUtc { get; set; }

    // Set only when SMTP accepted the message.
    public DateTime? SentAtUtc { get; set; }
}
