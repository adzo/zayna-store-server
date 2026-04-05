using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Products;

public class UpdateProductRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateProductValidator : Validator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);
    }
}

public class UpdateProductEndpoint : Endpoint<UpdateProductRequest, ProductDto>
{
    private readonly StoreDbContext _dbContext;

    public UpdateProductEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Put("/admin/products/{id}");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminProducts"));

        Summary(s =>
        {
            s.Summary = "Updates an existing product";
            s.Description = "Updates product details (name, description, price, stock, category, active status). Images are managed separately via image endpoints. Only accessible by administrators.";
            s.Response<ProductDto>(StatusCodes.Status200OK, "Product updated successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request, validation errors, or category not found");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Product not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(UpdateProductRequest req, CancellationToken ct)
    {
        var product = await _dbContext.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct);

        if (product == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Verify category exists
        var categoryExists = await _dbContext.Categories.AnyAsync(c => c.Id == req.CategoryId, ct);
        if (!categoryExists)
        {
            AddError("Category not found");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        product.Name = req.Name;
        product.Description = req.Description;
        product.Price = req.Price;
        product.StockQuantity = req.StockQuantity;
        product.CategoryId = req.CategoryId;
        product.IsActive = req.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        // Load category name
        var category = await _dbContext.Categories.FindAsync(new object[] { product.CategoryId }, ct);

        await Send.OkAsync(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            IsActive = product.IsActive,
            Images = product.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                IsMain = i.IsMain,
                DisplayOrder = i.DisplayOrder
            }).ToList(),
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        }, ct);
    }
}
