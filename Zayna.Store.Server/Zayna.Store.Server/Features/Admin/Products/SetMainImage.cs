using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Products;

public class SetMainImageRequest
{
    public int ImageId { get; set; }
}

public class SetMainImageEndpoint : Endpoint<SetMainImageRequest, ProductImageDto>
{
    private readonly StoreDbContext _dbContext;

    public SetMainImageEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Put("/admin/products/images/{imageId}/set-main");
        Roles(UserRoles.Admin);

        Summary(s =>
        {
            s.Summary = "Sets an image as the main product image";
            s.Description = "Marks a specific image as the main product image. Automatically unsets other main images for the same product. Only accessible by administrators.";
            s.Response<ProductImageDto>(StatusCodes.Status200OK, "Image set as main successfully");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Image not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(SetMainImageRequest req, CancellationToken ct)
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

        // Unset all other main images for this product
        foreach (var otherImage in product.Images.Where(i => i.Id != req.ImageId && i.IsMain))
        {
            otherImage.IsMain = false;
        }

        // Set this image as main
        image.IsMain = true;

        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new ProductImageDto
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            IsMain = image.IsMain,
            DisplayOrder = image.DisplayOrder
        }, ct);
    }
}
