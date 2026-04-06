using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;

namespace Zayna.Store.Server.Features.Public.Products;

public class GetPublicProductByIdRequest
{
    public int Id { get; set; }
}

public class PublicProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}

public class PublicProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<PublicProductImageDto> Images { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class GetPublicProductByIdEndpoint : Endpoint<GetPublicProductByIdRequest, PublicProductDetailDto>
{
    private readonly StoreDbContext _dbContext;

    public GetPublicProductByIdEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/products/{id}");
        AllowAnonymous();
        Tags("Products");
        Description(x => x.WithTags("Products"));

        Summary(s =>
        {
            s.Summary = "Retrieves product details by ID (public)";
            s.Description = "Returns full product details including all images and category information. Only returns active products. No authentication required.";
            s.Response<PublicProductDetailDto>(StatusCodes.Status200OK, "Product details retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Product not found or inactive");
        });
    }

    public override async Task HandleAsync(GetPublicProductByIdRequest req, CancellationToken ct)
    {
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.Id == req.Id && p.IsActive)
            .Select(p => new PublicProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryName = p.Category.Name,
                Images = p.Images.OrderBy(i => i.DisplayOrder).Select(i => new PublicProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain,
                    DisplayOrder = i.DisplayOrder
                }).ToList(),
                CreatedAt = p.CreatedAt
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
