using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Users.Admins;

public class DeleteAdminRequest
{
    public string Id { get; set; } = string.Empty;
}

public class DeleteAdminEndpoint : Endpoint<DeleteAdminRequest>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DeleteAdminEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Delete("/users/admins/{id}");
        Roles(UserRoles.Admin);
    }

    public override async Task HandleAsync(DeleteAdminRequest req, CancellationToken ct)
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
            AddError("User is not an admin");
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
