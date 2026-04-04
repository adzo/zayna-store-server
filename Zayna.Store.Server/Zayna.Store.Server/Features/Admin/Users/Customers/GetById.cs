using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Users.Customers;

public class GetCustomerByIdRequest
{
    public string Id { get; set; } = string.Empty;
}

public class GetCustomerByIdResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetCustomerByIdEndpoint : Endpoint<GetCustomerByIdRequest, GetCustomerByIdResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetCustomerByIdEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Get("/admin/users/customers/{id}");
        Roles(UserRoles.Admin);

        Summary(s =>
        {
            s.Summary = "Retrieves a customer user by ID";
            s.Description = "Returns detailed information about a specific customer user. Only accessible by administrators.";
            s.Response<GetCustomerByIdResponse>(StatusCodes.Status200OK, "Customer details retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Customer not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(GetCustomerByIdRequest req, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(req.Id);

        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Check if user is a customer
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRoles.Customer))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new GetCustomerByIdResponse
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
