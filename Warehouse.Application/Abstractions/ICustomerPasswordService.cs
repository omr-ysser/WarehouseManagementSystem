using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.DTOs;

namespace Warehouse.Application.Abstractions;

public interface ICustomerPasswordService
{
    Task QueueInvitationAsync(
        int riderId,
        CancellationToken ct);

    Task SetPasswordAsync(
        SetCustomerPasswordRequest request,
        CancellationToken ct);
}
