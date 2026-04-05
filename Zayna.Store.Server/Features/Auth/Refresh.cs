using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;
using Zayna.Store.Server.Services;

namespace Zayna.Store.Server.Features.Auth;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 1800; // 30 minutes in seconds
    public string TokenType { get; set; } = "Bearer";
}

public class RefreshTokenValidator : Validator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}

public class RefreshTokenEndpoint : Endpoint<RefreshTokenRequest, RefreshTokenResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly StoreDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public RefreshTokenEndpoint(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        StoreDbContext dbContext,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("/auth/refresh");
        AllowAnonymous();
        Description(x => x.WithTags("Auth"));

        Summary(s =>
        {
            s.Summary = "Refreshes an expired access token";
            s.Description = "Exchanges a valid refresh token for a new access token and refresh token. The old refresh token is revoked (single-use). Publicly accessible.";
            s.Response<RefreshTokenResponse>(StatusCodes.Status200OK, "Token refreshed successfully, new tokens returned");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or validation errors");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Invalid or expired refresh token");
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        // Find refresh token in database
        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == req.RefreshToken && !rt.IsRevoked, ct);

        if (storedToken == null)
        {
            await Send.UnauthorizedAsync(ct);
            AddError("Invalid or expired refresh token");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Check if token is expired
        if (storedToken.ExpiryDate < DateTime.UtcNow)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Check if user exists and is not deleted
        var user = storedToken.User;
        if (user.IsDeleted)
        {
            await Send.UnauthorizedAsync(ct);
            AddError("Invalid or expired refresh token");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Generate new tokens
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // Mark old refresh token as revoked
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken;

        // Get refresh token expiry from configuration
        var refreshTokenExpiryDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryDays");
        if (refreshTokenExpiryDays == 0) refreshTokenExpiryDays = 15;

        // Create new refresh token entity
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(ct);

        // Get access token expiry in seconds
        var accessTokenExpiryMinutes = _configuration.GetValue<int>("JwtSettings:AccessTokenExpiryMinutes");
        if (accessTokenExpiryMinutes == 0) accessTokenExpiryMinutes = 30;

        // Return response
        await Send.OkAsync(new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = accessTokenExpiryMinutes * 60,
            TokenType = "Bearer"
        }, ct);
    }
}
