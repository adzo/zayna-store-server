using FastEndpoints;
using FluentValidation;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Categories;

public class UpdateCategoryRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCategoryValidator : Validator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public class UpdateCategoryEndpoint : Endpoint<UpdateCategoryRequest, CategoryDto>
{
    private readonly StoreDbContext _dbContext;

    public UpdateCategoryEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Put("/admin/categories/{id}");
        Roles(UserRoles.Admin);

        Summary(s =>
        {
            s.Summary = "Updates an existing category";
            s.Description = "Updates the details of a product category. Only accessible by administrators.";
            s.Response<CategoryDto>(StatusCodes.Status200OK, "Category updated successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or validation errors");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Category not found");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(UpdateCategoryRequest req, CancellationToken ct)
    {
        var category = await _dbContext.Categories.FindAsync(new object[] { req.Id }, ct);

        if (category == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        category.Name = req.Name;
        category.Description = req.Description;

        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            ProductCount = category.Products.Count
        }, ct);
    }
}
