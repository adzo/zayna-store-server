using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Categories;

public class GetCategoryByIdRequest
{
    public int Id { get; set; }
}

public class GetCategoryByIdEndpoint : Endpoint<GetCategoryByIdRequest, CategoryDto>
{
    private readonly StoreDbContext _dbContext;

    public GetCategoryByIdEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/categories/{id}");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminCategories"));

        Summary(s =>
        {
            s.Summary = "Retrieves a category by ID";
            s.Description = "Returns detailed information about a specific category including product count. Only accessible by administrators.";
            s.Response<CategoryDto>(StatusCodes.Status200OK, "Category details retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Category not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(GetCategoryByIdRequest req, CancellationToken ct)
    {
        var category = await _dbContext.Categories
            .Where(c => c.Id == req.Id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                ProductCount = c.Products.Count
            })
            .FirstOrDefaultAsync(ct);

        if (category == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(category, ct);
    }
}
