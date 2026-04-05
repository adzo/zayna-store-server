using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Coupons;

public class UpdateCouponRequest
{
    public int Id { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? MaxUses { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
}

public class UpdateCouponValidator : Validator<UpdateCouponRequest>
{
    public UpdateCouponValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Coupon ID is required");

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

public class UpdateCouponEndpoint : Endpoint<UpdateCouponRequest, CouponDto>
{
    private readonly StoreDbContext _dbContext;

    public UpdateCouponEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Put("/admin/coupons/{id}");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminCoupons"));

        Summary(s =>
        {
            s.Summary = "Updates an existing coupon";
            s.Description = "Updates coupon details. Coupon code cannot be changed after creation. Only accessible by administrators.";
            s.Response<CouponDto>(StatusCodes.Status200OK, "Coupon updated successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or validation errors");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Coupon not found");
        });
    }

    public override async Task HandleAsync(UpdateCouponRequest req, CancellationToken ct)
    {
        var coupon = await _dbContext.Coupons
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (coupon == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        coupon.DiscountType = req.DiscountType;
        coupon.DiscountValue = req.DiscountValue;
        coupon.ValidFrom = req.ValidFrom;
        coupon.ValidTo = req.ValidTo;
        coupon.MaxUses = req.MaxUses;
        coupon.MinimumOrderAmount = req.MinimumOrderAmount;
        coupon.UpdatedAt = DateTime.UtcNow;

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
