using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;

namespace Warehouse.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityService _identity;
    private readonly ITokenService _tokens;

    public AuthService(
        IIdentityService identity,
        ITokenService tokens)
    {
        _identity = identity;
        _tokens = tokens;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _identity.ValidateCredentialsAsync(
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        if (user is null)
        {
            throw new AppException(401, "Invalid email or password.");
        }

        return _tokens.CreateToken(user);
    }
}
