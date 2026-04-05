using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Coupons;

public class DisableCouponRequest
{
    public int Id { get; set; }
}

public class DisableCouponEndpoint : Endpoint<DisableCouponRequest, CouponDto>
{
    private readonly StoreDbContext _dbContext;

    public DisableCouponEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Put("/admin/coupons/{id}/disable");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminCoupons"));

        Summary(s =>
        {
            s.Summary = "Disables a coupon";
            s.Description = "Marks a coupon as inactive, preventing it from being used. Only accessible by administrators.";
            s.Response<CouponDto>(StatusCodes.Status200OK, "Coupon disabled successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Coupon not found");
        });
    }

    public override async Task HandleAsync(DisableCouponRequest req, CancellationToken ct)
    {
        var coupon = await _dbContext.Coupons
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (coupon == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        coupon.IsActive = false;
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
