using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Categories;

public class DeleteCategoryRequest
{
    public int Id { get; set; }
}

public class DeleteCategoryEndpoint : Endpoint<DeleteCategoryRequest>
{
    private readonly StoreDbContext _dbContext;

    public DeleteCategoryEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete("/admin/categories/{id}");
        Roles(UserRoles.Admin);
        Tags("AdminCategories");
        Description(x => x.WithTags("AdminCategories"));

        Summary(s =>
        {
            s.Summary = "Deletes a category";
            s.Description = "Permanently deletes a category if it has no associated products. Only accessible by administrators.";
            s.Response(StatusCodes.Status204NoContent, "Category deleted successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Category has associated products and cannot be deleted");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Category not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(DeleteCategoryRequest req, CancellationToken ct)
    {
        var category = await _dbContext.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (category == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Check if category has products
        if (category.Products.Any())
        {
            AddError("Cannot delete category with associated products");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
