using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Coupons;

public class GetCouponByIdRequest
{
    public int Id { get; set; }
}

public class GetCouponByIdEndpoint : Endpoint<GetCouponByIdRequest, CouponDto>
{
    private readonly StoreDbContext _dbContext;

    public GetCouponByIdEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/coupons/{id}");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminCoupons"));

        Summary(s =>
        {
            s.Summary = "Retrieves coupon details by ID";
            s.Description = "Returns detailed information about a specific coupon including usage statistics. Only accessible by administrators.";
            s.Response<CouponDto>(StatusCodes.Status200OK, "Coupon retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Coupon not found");
        });
    }

    public override async Task HandleAsync(GetCouponByIdRequest req, CancellationToken ct)
    {
        var coupon = await _dbContext.Coupons
            .Where(c => c.Id == req.Id)
            .Select(c => new CouponDto
            {
                Id = c.Id,
                Code = c.Code,
                DiscountType = c.DiscountType,
                DiscountValue = c.DiscountValue,
                ValidFrom = c.ValidFrom,
                ValidTo = c.ValidTo,
                IsActive = c.IsActive,
                MaxUses = c.MaxUses,
                CurrentUses = c.CurrentUses,
                MinimumOrderAmount = c.MinimumOrderAmount,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (coupon == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(coupon, ct);
    }
}
