using FastEndpoints;
using FluentValidation;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Categories;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ProductCount { get; set; }
}

public class CreateCategoryValidator : Validator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public class CreateCategoryEndpoint : Endpoint<CreateCategoryRequest, CategoryDto>
{
    private readonly StoreDbContext _dbContext;

    public CreateCategoryEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/admin/categories");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminCategories"));

        Summary(s =>
        {
            s.Summary = "Creates a new product category";
            s.Description = "Creates a new category for organizing products. Only accessible by administrators.";
            s.Response<CategoryDto>(StatusCodes.Status200OK, "Category created successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or validation errors");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CreateCategoryRequest req, CancellationToken ct)
    {
        var category = new Category
        {
            Name = req.Name,
            Description = req.Description,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            ProductCount = 0
        }, ct);
    }
}
