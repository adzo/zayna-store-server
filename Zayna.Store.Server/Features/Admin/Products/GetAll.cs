using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Products;

public class GetAllProductsResponse
{
    public List<ProductDto> Products { get; set; } = new();
}

public class GetAllProductsEndpoint : EndpointWithoutRequest<GetAllProductsResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetAllProductsEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/products");
        Roles(UserRoles.Admin);
        Tags("AdminProducts");
        Description(x => x.WithTags("AdminProducts"));

        Summary(s =>
        {
            s.Summary = "Retrieves all products";
            s.Description = "Returns a list of all products with full details including all images and category information. Only accessible by administrators.";
            s.Response<GetAllProductsResponse>(StatusCodes.Status200OK, "List of products retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var products = await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
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
            .ToListAsync(ct);

        await Send.OkAsync(new GetAllProductsResponse
        {
            Products = products
        }, ct);
    }
}
