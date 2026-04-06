using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Products;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductImageInput> Images { get; set; } = new();
}

public class ProductImageInput
{
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateProductValidator : Validator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);

        RuleFor(x => x.Images)
            .NotEmpty()
            .WithMessage("At least one image is required");

        RuleFor(x => x.Images)
            .Must(images => images.Count(i => i.IsMain) == 1)
            .When(x => x.Images.Any())
            .WithMessage("Exactly one image must be marked as main");
    }
}

public class CreateProductEndpoint : Endpoint<CreateProductRequest, ProductDto>
{
    private readonly StoreDbContext _dbContext;

    public CreateProductEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/admin/products");
        Roles(UserRoles.Admin);
        Tags("AdminProducts");
        Description(x => x.WithTags("AdminProducts"));

        Summary(s =>
        {
            s.Summary = "Creates a new product with images";
            s.Description = "Creates a new product with multiple images. At least one image is required and exactly one must be marked as main. Only accessible by administrators.";
            s.Response<ProductDto>(StatusCodes.Status200OK, "Product created successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request, validation errors, or category not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        // Verify category exists
        var categoryExists = await _dbContext.Categories.AnyAsync(c => c.Id == req.CategoryId, ct);
        if (!categoryExists)
        {
            AddError("Category not found");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var product = new Product
        {
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            StockQuantity = req.StockQuantity,
            CategoryId = req.CategoryId,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        // Add images
        foreach (var imageInput in req.Images)
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl = imageInput.ImageUrl,
                IsMain = imageInput.IsMain,
                DisplayOrder = imageInput.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            });
        }

        _dbContext.Products.Add(product);
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
            Images = product.Images.Select(i => new ProductImageDto
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
