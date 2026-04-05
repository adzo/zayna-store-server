using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Products;

public class DeleteProductRequest
{
    public int Id { get; set; }
}

public class DeleteProductEndpoint : Endpoint<DeleteProductRequest>
{
    private readonly StoreDbContext _dbContext;

    public DeleteProductEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete("/admin/products/{id}");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminProducts"));

        Summary(s =>
        {
            s.Summary = "Deletes a product";
            s.Description = "Permanently deletes a product and all associated images. Only accessible by administrators.";
            s.Response(StatusCodes.Status204NoContent, "Product deleted successfully");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Product not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(DeleteProductRequest req, CancellationToken ct)
    {
        var product = await _dbContext.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct);

        if (product == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
