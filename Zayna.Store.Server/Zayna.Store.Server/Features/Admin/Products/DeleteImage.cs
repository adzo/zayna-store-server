using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Products;

public class DeleteImageRequest
{
    public int ImageId { get; set; }
}

public class DeleteImageEndpoint : Endpoint<DeleteImageRequest>
{
    private readonly StoreDbContext _dbContext;

    public DeleteImageEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete("/admin/products/images/{imageId}");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminProducts"));

        Summary(s =>
        {
            s.Summary = "Deletes a product image";
            s.Description = "Removes an image from a product. Cannot delete the last remaining image. If deleting the main image, automatically promotes another image to main. Only accessible by administrators.";
            s.Response(StatusCodes.Status204NoContent, "Image deleted successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Cannot delete the only remaining image");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Image not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(DeleteImageRequest req, CancellationToken ct)
    {
        var image = await _dbContext.ProductImages
            .Include(i => i.Product)
            .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(i => i.Id == req.ImageId, ct);

        if (image == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var product = image.Product;

        // Check if this is the only image
        if (product.Images.Count == 1)
        {
            AddError("Cannot delete the only image. Product must have at least one image.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var wasMain = image.IsMain;

        _dbContext.ProductImages.Remove(image);
        await _dbContext.SaveChangesAsync(ct);

        // If deleted image was main, promote another image to main
        if (wasMain)
        {
            var newMainImage = product.Images
                .Where(i => i.Id != req.ImageId)
                .OrderBy(i => i.DisplayOrder)
                .FirstOrDefault();

            if (newMainImage != null)
            {
                newMainImage.IsMain = true;
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        await Send.NoContentAsync(ct);
    }
}
