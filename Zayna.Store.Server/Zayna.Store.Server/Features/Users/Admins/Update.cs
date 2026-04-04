using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Users.Admins;

public class UpdateAdminRequest
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class UpdateAdminResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateAdminValidator : Validator<UpdateAdminRequest>
{
    public UpdateAdminValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.FirstName)
            .NotEmpty();

        RuleFor(x => x.LastName)
            .NotEmpty();

        RuleFor(x => x.PhoneNumber)
            .NotEmpty();

        RuleFor(x => x.Address)
            .NotEmpty();
    }
}

public class UpdateAdminEndpoint : Endpoint<UpdateAdminRequest, UpdateAdminResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateAdminEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Put("/users/admins/{id}");
        Roles(UserRoles.Admin);
    }

    public override async Task HandleAsync(UpdateAdminRequest req, CancellationToken ct)
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

        user.Email = req.Email;
        user.UserName = req.Email;
        user.FirstName = req.FirstName;
        user.LastName = req.LastName;
        user.PhoneNumber = req.PhoneNumber;
        user.Address = req.Address;

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

        await Send.OkAsync(new UpdateAdminResponse
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
