using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Users.Customers;

public class UpdateCustomerRequest
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class UpdateCustomerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateCustomerValidator : Validator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
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

public class UpdateCustomerEndpoint : Endpoint<UpdateCustomerRequest, UpdateCustomerResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateCustomerEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Put("/users/customers/{id}");
        Roles(UserRoles.Admin);
    }

    public override async Task HandleAsync(UpdateCustomerRequest req, CancellationToken ct)
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

        await Send.OkAsync(new UpdateCustomerResponse
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
