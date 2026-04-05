using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;

namespace Zayna.Store.Server.Features.Public.Products;

public class GetPublicProductsRequest
{
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PublicProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? MainImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class GetPublicProductsResponse
{
    public List<PublicProductListDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class GetPublicProductsEndpoint : Endpoint<GetPublicProductsRequest, GetPublicProductsResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetPublicProductsEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/products");
        AllowAnonymous();
        Description(x => x.WithTags("Products"));

        Summary(s =>
        {
            s.Summary = "Retrieves active products with pagination (public)";
            s.Description = "Returns a paginated list of active products with main image only. Supports filtering by category and text search. No authentication required.";
            s.Response<GetPublicProductsResponse>(StatusCodes.Status200OK, "List of products retrieved successfully with pagination info");
        });
    }

    public override async Task HandleAsync(GetPublicProductsRequest req, CancellationToken ct)
    {
        var query = _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsActive);

        // Filter by category
        if (req.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == req.CategoryId.Value);
        }

        // Search by name or description
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var searchLower = req.Search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower) ||
                                    p.Description.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(p => new PublicProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                MainImageUrl = p.Images.FirstOrDefault(i => i.IsMain) != null
                    ? p.Images.FirstOrDefault(i => i.IsMain)!.ImageUrl
                    : p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault() != null
                        ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()!.ImageUrl
                        : null,
                CategoryName = p.Category.Name
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize);

        await Send.OkAsync(new GetPublicProductsResponse
        {
            Products = products,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = totalPages
        }, ct);
    }
}
