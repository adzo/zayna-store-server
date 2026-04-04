using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Users.Admins;

public class GetAdminByIdRequest
{
    public string Id { get; set; } = string.Empty;
}

public class GetAdminByIdResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetAdminByIdEndpoint : Endpoint<GetAdminByIdRequest, GetAdminByIdResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetAdminByIdEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Get("/users/admins/{id}");
        Roles(UserRoles.Admin);
    }

    public override async Task HandleAsync(GetAdminByIdRequest req, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(req.Id);

        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Check if user is an admin
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRoles.Admin))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new GetAdminByIdResponse
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber!,
            Address = user.Address,
            CreatedAt = user.CreatedAt
        }, ct);
    }
}
