using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Coupons;

public class ValidateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
}

public class ValidateCouponResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public CouponValidationDto? Coupon { get; set; }
}

public class CouponValidationDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal { get; set; }
}

public class ValidateCouponValidator : Validator<ValidateCouponRequest>
{
    public ValidateCouponValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Coupon code is required");

        RuleFor(x => x.OrderTotal)
            .GreaterThan(0)
            .WithMessage("Order total must be greater than 0");
    }
}

public class ValidateCouponEndpoint : Endpoint<ValidateCouponRequest, ValidateCouponResponse>
{
    private readonly StoreDbContext _dbContext;

    public ValidateCouponEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/coupons/validate");
        Roles(UserRoles.Admin, UserRoles.Customer);
        Description(x => x.WithTags("Coupons"));

        Summary(s =>
        {
            s.Summary = "Validates a coupon code";
            s.Description = "Checks if a coupon code is valid for the authenticated user and calculates the discount amount. Returns validation status, discount details, and final order total.";
            s.Response<ValidateCouponResponse>(StatusCodes.Status200OK, "Coupon validation result returned");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or validation errors");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
        });
    }

    public override async Task HandleAsync(ValidateCouponRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Find coupon (case-insensitive)
        var coupon = await _dbContext.Coupons
            .FirstOrDefaultAsync(c => c.Code.ToLower() == req.Code.ToLower(), ct);

        if (coupon == null)
        {
            await Send.OkAsync(new ValidateCouponResponse
            {
                IsValid = false,
                Message = "Coupon code does not exist"
            }, ct);
            return;
        }

        // Check if coupon is active
        if (!coupon.IsActive)
        {
            await Send.OkAsync(new ValidateCouponResponse
            {
                IsValid = false,
                Message = "This coupon is no longer active"
            }, ct);
            return;
        }

        // Check validity dates
        var now = DateTime.UtcNow;
        if (now < coupon.ValidFrom)
        {
            await Send.OkAsync(new ValidateCouponResponse
            {
                IsValid = false,
                Message = $"This coupon is not valid until {coupon.ValidFrom:yyyy-MM-dd}"
            }, ct);
            return;
        }

        if (now > coupon.ValidTo)
        {
            await Send.OkAsync(new ValidateCouponResponse
            {
                IsValid = false,
                Message = "This coupon has expired"
            }, ct);
            return;
        }

        // Check if user already used this coupon
        var alreadyUsed = await _dbContext.CouponUsages
            .AnyAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId, ct);

        if (alreadyUsed)
        {
            await Send.OkAsync(new ValidateCouponResponse
            {
                IsValid = false,
                Message = "You have already used this coupon"
            }, ct);
            return;
        }

        // Check max uses
        if (coupon.MaxUses.HasValue && coupon.CurrentUses >= coupon.MaxUses.Value)
        {
            await Send.OkAsync(new ValidateCouponResponse
            {
                IsValid = false,
                Message = "This coupon has reached its maximum number of uses"
            }, ct);
            return;
        }

        // Check minimum order amount
        if (coupon.MinimumOrderAmount.HasValue && req.OrderTotal < coupon.MinimumOrderAmount.Value)
        {
            await Send.OkAsync(new ValidateCouponResponse
            {
                IsValid = false,
                Message = $"Minimum order amount of {coupon.MinimumOrderAmount.Value:C} required to use this coupon"
            }, ct);
            return;
        }

        // Calculate discount
        decimal discountAmount = coupon.DiscountType == DiscountType.Percentage
            ? req.OrderTotal * (coupon.DiscountValue / 100)
            : coupon.DiscountValue;

        // Ensure discount doesn't exceed order total
        if (discountAmount > req.OrderTotal)
        {
            discountAmount = req.OrderTotal;
        }

        var finalTotal = req.OrderTotal - discountAmount;

        await Send.OkAsync(new ValidateCouponResponse
        {
            IsValid = true,
            Message = "Coupon is valid",
            Coupon = new CouponValidationDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                DiscountType = coupon.DiscountType,
                DiscountValue = coupon.DiscountValue,
                DiscountAmount = discountAmount,
                FinalTotal = finalTotal
            }
        }, ct);
    }
}
