using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Users.Admins;

public class GetAllAdminsResponse
{
    public List<AdminDto> Admins { get; set; } = new();
}

public class AdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetAllAdminsEndpoint : EndpointWithoutRequest<GetAllAdminsResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetAllAdminsEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Get("/admin/users/admins");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminUsers"));

        Summary(s =>
        {
            s.Summary = "Retrieves all administrator users";
            s.Description = "Returns a list of all users with the Admin role. Only accessible by administrators.";
            s.Response<GetAllAdminsResponse>(StatusCodes.Status200OK, "List of admins retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);

        var adminDtos = admins.Select(a => new AdminDto
        {
            Id = a.Id,
            Email = a.Email!,
            FirstName = a.FirstName,
            LastName = a.LastName,
            PhoneNumber = a.PhoneNumber!,
            Address = a.Address,
            CreatedAt = a.CreatedAt
        }).ToList();

        await Send.OkAsync(new GetAllAdminsResponse
        {
            Admins = adminDtos
        }, ct);
    }
}
