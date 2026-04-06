using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Products;

public class GetProductByIdRequest
{
    public int Id { get; set; }
}

public class GetProductByIdEndpoint : Endpoint<GetProductByIdRequest, ProductDto>
{
    private readonly StoreDbContext _dbContext;

    public GetProductByIdEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/products/{id}");
        Roles(UserRoles.Admin);
        Tags("AdminProducts");
        Description(x => x.WithTags("AdminProducts"));

        Summary(s =>
        {
            s.Summary = "Retrieves a product by ID";
            s.Description = "Returns detailed information about a specific product including all images and category. Only accessible by administrators.";
            s.Response<ProductDto>(StatusCodes.Status200OK, "Product details retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Product not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(GetProductByIdRequest req, CancellationToken ct)
    {
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.Id == req.Id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                IsActive = p.IsActive,
                Images = p.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain,
                    DisplayOrder = i.DisplayOrder
                }).ToList(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (product == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(product, ct);
    }
}
