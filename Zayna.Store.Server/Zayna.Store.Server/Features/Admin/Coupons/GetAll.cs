using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Coupons;

public class GetAllCouponsResponse
{
    public List<CouponDto> Coupons { get; set; } = new();
}

public class GetAllCouponsEndpoint : EndpointWithoutRequest<GetAllCouponsResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetAllCouponsEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/coupons");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminCoupons"));

        Summary(s =>
        {
            s.Summary = "Retrieves all coupons";
            s.Description = "Returns a list of all discount coupons with usage statistics. Only accessible by administrators.";
            s.Response<GetAllCouponsResponse>(StatusCodes.Status200OK, "List of coupons retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var coupons = await _dbContext.Coupons
            .OrderByDescending(c => c.CreatedAt)
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
            .ToListAsync(ct);

        await Send.OkAsync(new GetAllCouponsResponse
        {
            Coupons = coupons
        }, ct);
    }
}
