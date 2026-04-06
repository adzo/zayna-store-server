using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Coupons;

public class EnableCouponRequest
{
    public int Id { get; set; }
}

public class EnableCouponEndpoint : Endpoint<EnableCouponRequest, CouponDto>
{
    private readonly StoreDbContext _dbContext;

    public EnableCouponEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Put("/admin/coupons/{id}/enable");
        Roles(UserRoles.Admin);
        Tags("AdminCoupons");
        Description(x => x.WithTags("AdminCoupons"));

        Summary(s =>
        {
            s.Summary = "Enables a coupon";
            s.Description = "Marks a coupon as active, allowing it to be used if other conditions are met. Only accessible by administrators.";
            s.Response<CouponDto>(StatusCodes.Status200OK, "Coupon enabled successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Coupon not found");
        });
    }

    public override async Task HandleAsync(EnableCouponRequest req, CancellationToken ct)
    {
        var coupon = await _dbContext.Coupons
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (coupon == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        coupon.IsActive = true;
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
