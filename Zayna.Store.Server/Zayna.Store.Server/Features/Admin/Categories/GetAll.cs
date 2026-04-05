using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;
using Zayna.Store.Server.Models;

namespace Zayna.Store.Server.Features.Admin.Categories;

public class GetAllCategoriesResponse
{
    public List<MinimalItem> Categories { get; set; } = new();
}

public class GetAllCategoriesEndpoint : EndpointWithoutRequest<GetAllCategoriesResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetAllCategoriesEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/categories");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminCategories"));

        Summary(s =>
        {
            s.Summary = "Retrieves all product categories";
            s.Description = "Returns a list of all categories with product counts. Only accessible by administrators.";
            s.Response<GetAllCategoriesResponse>(StatusCodes.Status200OK, "List of categories retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var categories = await _dbContext.Categories
            .Select(c => new MinimalItem(c.Id, c.Name))
            .ToListAsync(ct);

        await Send.OkAsync(new GetAllCategoriesResponse
        {
            Categories = categories
        }, ct);
    }
}
