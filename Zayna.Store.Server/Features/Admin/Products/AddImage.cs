using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Products;

public class AddImageRequest
{
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}

public class AddImageValidator : Validator<AddImageRequest>
{
    public AddImageValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0);

        RuleFor(x => x.ImageUrl)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class AddImageEndpoint : Endpoint<AddImageRequest, ProductImageDto>
{
    private readonly StoreDbContext _dbContext;

    public AddImageEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/admin/products/{productId}/images");
        Roles(UserRoles.Admin);
        Tags("AdminProducts");
        Description(x => x.WithTags("AdminProducts"));

        Summary(s =>
        {
            s.Summary = "Adds an image to a product";
            s.Description = "Adds a new image to an existing product. If marked as main, automatically unsets other main images. Only accessible by administrators.";
            s.Response<ProductImageDto>(StatusCodes.Status200OK, "Image added successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or validation errors");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Product not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(AddImageRequest req, CancellationToken ct)
    {
        var product = await _dbContext.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);

        if (product == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // If setting as main, unset other main images
        if (req.IsMain)
        {
            foreach (var existingImage in product.Images.Where(i => i.IsMain))
            {
                existingImage.IsMain = false;
            }
        }

        var newImage = new ProductImage
        {
            ProductId = req.ProductId,
            ImageUrl = req.ImageUrl,
            IsMain = req.IsMain,
            DisplayOrder = req.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ProductImages.Add(newImage);
        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new ProductImageDto
        {
            Id = newImage.Id,
            ImageUrl = newImage.ImageUrl,
            IsMain = newImage.IsMain,
            DisplayOrder = newImage.DisplayOrder
        }, ct);
    }
}
