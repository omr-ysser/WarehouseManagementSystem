using FluentValidation;
using Microsoft.Extensions.Logging;
using Warehouse.Application.Abstractions;
using Warehouse.Application.DTOs;
using Warehouse.Application.Exceptions;

namespace Warehouse.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityService _identity;
    private readonly ITokenService _tokens;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IIdentityService identity,
        ITokenService tokens,
        IValidator<LoginRequest> loginValidator,
        ILogger<AuthService> logger)
    {
        _identity = identity;
        _tokens = tokens;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var user = await _identity.ValidateCredentialsAsync(
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        if (user is null)
        {
            _logger.LogWarning(
                "Login rejected because credentials were invalid");

            throw new AppException(
                401,
                "Invalid email or password.");
        }

        var response = _tokens.CreateToken(user);

        _logger.LogInformation(
            "Login succeeded for user {UserId}",
            user.Id);

        return response;
    }
}