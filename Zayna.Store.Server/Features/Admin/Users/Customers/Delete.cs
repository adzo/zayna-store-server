using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Users.Customers;

public class DeleteCustomerRequest
{
    public string Id { get; set; } = string.Empty;
}

public class DeleteCustomerEndpoint : Endpoint<DeleteCustomerRequest>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DeleteCustomerEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Delete("/admin/users/customers/{id}");
        Roles(UserRoles.Admin);
        Tags("AdminUsers");
        Description(x => x.WithTags("AdminUsers"));

        Summary(s =>
        {
            s.Summary = "Soft deletes a customer user";
            s.Description = "Marks a customer user as deleted without permanently removing the record. Only accessible by administrators.";
            s.Response(StatusCodes.Status204NoContent, "Customer deleted successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or user is not a customer");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Customer not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(DeleteCustomerRequest req, CancellationToken ct)
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
            AddError("User is not a customer");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
