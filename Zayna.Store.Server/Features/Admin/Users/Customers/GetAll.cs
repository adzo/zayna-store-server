using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Users.Customers;

public class GetAllCustomersResponse
{
    public List<CustomerDto> Customers { get; set; } = new();
}

public class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetAllCustomersEndpoint : EndpointWithoutRequest<GetAllCustomersResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetAllCustomersEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Get("/admin/users/customers");
        Roles(UserRoles.Admin);
        Tags("AdminUsers");
        Description(x => x.WithTags("AdminUsers"));

        Summary(s =>
        {
            s.Summary = "Retrieves all customer users";
            s.Description = "Returns a list of all users with the Customer role. Only accessible by administrators.";
            s.Response<GetAllCustomersResponse>(StatusCodes.Status200OK, "List of customers retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var customers = await _userManager.GetUsersInRoleAsync(UserRoles.Customer);

        var customerDtos = customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            Email = c.Email!,
            FirstName = c.FirstName,
            LastName = c.LastName,
            PhoneNumber = c.PhoneNumber!,
            Address = c.Address,
            CreatedAt = c.CreatedAt
        }).ToList();

        await Send.OkAsync(new GetAllCustomersResponse
        {
            Customers = customerDtos
        }, ct);
    }
}
