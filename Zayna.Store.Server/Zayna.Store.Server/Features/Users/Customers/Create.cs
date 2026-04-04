using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Users.Customers;

public class CreateCustomerRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class CreateCustomerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateCustomerValidator : Validator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();

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

public class CreateCustomerEndpoint : Endpoint<CreateCustomerRequest, CreateCustomerResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateCustomerEndpoint(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Post("/users/customers");
        Roles(UserRoles.Admin);
    }

    public override async Task HandleAsync(CreateCustomerRequest req, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            PhoneNumber = req.PhoneNumber,
            Address = req.Address,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, req.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await _userManager.AddToRoleAsync(user, UserRoles.Customer);

        await Send.OkAsync(new CreateCustomerResponse
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
