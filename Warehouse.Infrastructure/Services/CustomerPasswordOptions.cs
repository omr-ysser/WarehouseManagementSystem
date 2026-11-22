using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warehouse.Infrastructure.Services;

public sealed class CustomerPasswordOptions
{
    public const string SectionName = "CustomerPasswords";

    public bool EmailEnabled { get; set; }

    public string PasswordPageUrl { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string DataProtectionKeysPath { get; set; } =
        "App_Data/DataProtectionKeys";

    public CustomerSmtpOptions Smtp { get; set; } = new();
}

public sealed class CustomerSmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }
}
