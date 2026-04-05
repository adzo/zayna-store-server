using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;

namespace Zayna.Store.Server.Features.Public.Categories;

public class PublicCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GetPublicCategoriesResponse
{
    public List<PublicCategoryDto> Categories { get; set; } = new();
}

public class GetPublicCategoriesEndpoint : EndpointWithoutRequest<GetPublicCategoriesResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetPublicCategoriesEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/categories");
        AllowAnonymous();
        Description(x => x.WithTags("Categories"));

        Summary(s =>
        {
            s.Summary = "Retrieves all product categories (public)";
            s.Description = "Returns a minimal list of all categories (ID and name only) for public browsing. No authentication required.";
            s.Response<GetPublicCategoriesResponse>(StatusCodes.Status200OK, "List of categories retrieved successfully");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var categories = await _dbContext.Categories
            .Select(c => new PublicCategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync(ct);

        await Send.OkAsync(new GetPublicCategoriesResponse
        {
            Categories = categories
        }, ct);
    }
}
