using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;
using Zayna.Store.Server.Services;

namespace Zayna.Store.Server.Features.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 1800; // 30 minutes in seconds
    public string TokenType { get; set; } = "Bearer";
    public UserDto User { get; set; } = new();
}

public class LoginValidator : Validator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);
    }
}

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly StoreDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public LoginEndpoint(
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
        Post("/auth/login");
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Authenticates a user and returns JWT tokens";
            s.Description = "Validates user credentials and returns an access token (30 minutes) and refresh token (15 days) along with user information and roles. Publicly accessible.";
            s.Response<LoginResponse>(StatusCodes.Status200OK, "Login successful, tokens returned");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or validation errors");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Invalid email or password");
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Validate password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!isPasswordValid)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Get refresh token expiry from configuration
        var refreshTokenExpiryDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryDays");
        if (refreshTokenExpiryDays == 0)
        {
            refreshTokenExpiryDays = 15;
        }

        // Save refresh token to database
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(ct);

        // Get access token expiry in seconds
        var accessTokenExpiryMinutes = _configuration.GetValue<int>("JwtSettings:AccessTokenExpiryMinutes");
        if (accessTokenExpiryMinutes == 0) accessTokenExpiryMinutes = 30;

        // Return response
        await Send.OkAsync(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = accessTokenExpiryMinutes * 60,
            TokenType = "Bearer",
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            }
        }, ct);
    }
}
