using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Coupons;

public class CreateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? MaxUses { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
}

public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
    public int? MaxUses { get; set; }
    public int CurrentUses { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCouponValidator : Validator<CreateCouponRequest>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Coupon code must contain only uppercase letters and numbers");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0)
            .WithMessage("Discount value must be greater than 0");

        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage)
            .WithMessage("Percentage discount cannot exceed 100%");

        RuleFor(x => x.ValidFrom)
            .NotEmpty()
            .WithMessage("Valid from date is required");

        RuleFor(x => x.ValidTo)
            .NotEmpty()
            .GreaterThan(x => x.ValidFrom)
            .WithMessage("Valid to date must be after valid from date");

        RuleFor(x => x.MaxUses)
            .GreaterThan(0)
            .When(x => x.MaxUses.HasValue)
            .WithMessage("Max uses must be greater than 0");

        RuleFor(x => x.MinimumOrderAmount)
            .GreaterThan(0)
            .When(x => x.MinimumOrderAmount.HasValue)
            .WithMessage("Minimum order amount must be greater than 0");
    }
}

public class CreateCouponEndpoint : Endpoint<CreateCouponRequest, CouponDto>
{
    private readonly StoreDbContext _dbContext;

    public CreateCouponEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/admin/coupons");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminCoupons"));

        Summary(s =>
        {
            s.Summary = "Creates a new coupon code";
            s.Description = "Creates a new discount coupon with validation rules and usage limits. Code must be unique and contain only uppercase letters and numbers. Only accessible by administrators.";
            s.Response<CouponDto>(StatusCodes.Status200OK, "Coupon created successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request, validation errors, or coupon code already exists");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CreateCouponRequest req, CancellationToken ct)
    {
        // Check if coupon code already exists (case-insensitive)
        var codeExists = await _dbContext.Coupons
            .AnyAsync(c => c.Code.ToLower() == req.Code.ToLower(), ct);

        if (codeExists)
        {
            AddError("Coupon code already exists");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var coupon = new Coupon
        {
            Code = req.Code.ToUpper(),
            DiscountType = req.DiscountType,
            DiscountValue = req.DiscountValue,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            IsActive = true,
            MaxUses = req.MaxUses,
            MinimumOrderAmount = req.MinimumOrderAmount,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Coupons.Add(coupon);
        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new CouponDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountType = coupon.DiscountType,
            DiscountValue = coupon.DiscountValue,
            ValidFrom = coupon.ValidFrom,
            ValidTo = coupon.ValidTo,
            IsActive = coupon.IsActive,
            MaxUses = coupon.MaxUses,
            CurrentUses = coupon.CurrentUses,
            MinimumOrderAmount = coupon.MinimumOrderAmount,
            CreatedAt = coupon.CreatedAt,
            UpdatedAt = coupon.UpdatedAt
        }, ct);
    }
}
