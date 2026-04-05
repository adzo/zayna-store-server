# Zayna Store Server - Project Documentation

## Project Overview

E-commerce API built with ASP.NET Core using FastEndpoints framework. Supports user management, product catalog, categories, and order processing with JWT authentication and role-based authorization.

## Tech Stack

- **Framework**: ASP.NET Core 9.0
- **Endpoint Framework**: FastEndpoints 8.1.0
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10.0
- **Database Provider**: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1
- **Authentication**: ASP.NET Core Identity + JWT Bearer
- **Validation**: FluentValidation (via FastEndpoints)
- **Documentation**: Swagger/OpenAPI (via FastEndpoints)

## Project Structure

```
Zayna.Store.Server/
├── Data/
│   ├── StoreDbContext.cs          # EF Core DbContext
│   ├── DatabaseConfig.cs          # Database configuration class
│   ├── DbSeeder.cs                # Seeds default admin, roles, categories, products
│   └── Migrations/                # EF Core migrations
├── Entities/
│   ├── ApplicationUser.cs         # Identity user + UserRoles constants
│   ├── Category.cs
│   ├── Product.cs
│   ├── ProductImage.cs            # Multiple images per product
│   ├── Order.cs                   # OrderStatus enum included
│   ├── OrderItem.cs
│   └── RefreshToken.cs            # JWT refresh token management
├── Features/                      # Organized by feature (vertical slices)
│   ├── Admin/                     # Admin-only endpoints (role-protected)
│   │   ├── Categories/            # CRUD for categories
│   │   ├── Orders/                # Order management (list, details, status updates)
│   │   ├── Products/              # CRUD for products + image management
│   │   └── Users/
│   │       ├── Admins/            # Admin user management
│   │       └── Customers/         # Customer user management
│   ├── Auth/                      # Authentication endpoints
│   │   ├── Login.cs               # POST /auth/login
│   │   └── Refresh.cs             # POST /auth/refresh
│   ├── Orders/                    # Customer orders
│   │   ├── PlaceOrder.cs          # POST /orders
│   │   ├── GetAll.cs              # GET /orders (user's own orders)
│   │   └── GetById.cs             # GET /orders/{id} (user's own order)
│   └── Public/                    # Anonymous access endpoints
│       ├── Categories/            # Browse categories
│       └── Products/              # Browse products
├── Services/
│   └── TokenService.cs            # JWT token generation
├── Program.cs                     # Application startup and configuration
└── appsettings.json               # Configuration
```

## Authentication & Authorization

### JWT Configuration

**Token Generation** (TokenService.cs):
- Access token: 30 minutes expiry
- Refresh token: 15 days expiry (single-use, rotated on refresh)
- Algorithm: HS256
- Claims:
  - `sub`: User ID
  - `email`: User email
  - `given_name`: First name
  - `family_name`: Last name
  - `jti`: Token unique identifier
  - `role`: User role(s) - **IMPORTANT: Use short form "role", not ClaimTypes.Role**

**Configuration** (Program.cs):
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = key
        };
    });
```

**Swagger JWT Setup**:
```csharp
s.AddAuth("Bearer", new OpenApiSecurityScheme
{
    Type = OpenApiSecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    Description = "Enter your JWT token"
});
```

### Roles

- `UserRoles.Admin` - Full system access
- `UserRoles.Customer` - Customer access (orders, profile)

**Role Usage in Endpoints**:
```csharp
Roles(UserRoles.Admin);                        // Admin only
Roles(UserRoles.Admin, UserRoles.Customer);    // Both roles
AllowAnonymous();                              // Public access
```

### Getting User ID in Endpoints

```csharp
var userId = User.FindFirst("sub")?.Value;
if (string.IsNullOrEmpty(userId))
{
    await Send.UnauthorizedAsync(ct);
    return;
}
```

## Database Configuration

### Connection String

Uses `DatabaseConfig` class instead of raw connection strings:

```json
{
  "DatabaseConfig": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "ZaynaStore",
    "UserName": "postgres",
    "Password": "mysecretpassword",
    "MinimumPoolSize": 1,
    "MaximumPoolSize": 100
  }
}
```

**DatabaseConfig.cs** generates the connection string:
```csharp
public string ConnectionString =>
    $"Host={Host};Port={Port};Database={Database};Username={UserName};Password={Password};Maximum Pool Size={MaximumPoolSize};Minimum Pool Size={MinimumPoolSize}";
```

### Database Setup

```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update
```

### Seeding

`DbSeeder.cs` runs automatically on app startup:
- Creates Admin and Customer roles
- Creates default admin user (admin@zayna.com)
- Seeds 5 categories
- Seeds 100 products with Unsplash images

## FastEndpoints Patterns

### Endpoint Structure

```csharp
public class MyEndpoint : Endpoint<MyRequest, MyResponse>
{
    private readonly StoreDbContext _dbContext;

    public MyEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/my-route/{id}");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("MyTag")); // IMPORTANT: For Swagger/OpenAPI grouping and TypeScript client generation

        Summary(s =>
        {
            s.Summary = "Short description";
            s.Description = "Detailed description";
            s.Response<MyResponse>(StatusCodes.Status200OK, "Success message");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Error message");
        });
    }

    public override async Task HandleAsync(MyRequest req, CancellationToken ct)
    {
        // Implementation
        await Send.OkAsync(response, ct);
    }
}
```

### Swagger Tags (for Client Generation)

**CRITICAL**: All endpoints MUST have a `Description(x => x.WithTags("..."))` call for proper Swagger grouping and TypeScript/client generation.

**Tag Naming Convention** (NO dots, use PascalCase):
- `Description(x => x.WithTags("AdminOrders"))` - Admin order management endpoints
- `Description(x => x.WithTags("AdminUsers"))` - Admin user management (both admins and customers)
- `Description(x => x.WithTags("AdminProducts"))` - Admin product management
- `Description(x => x.WithTags("AdminCategories"))` - Admin category management
- `Description(x => x.WithTags("Auth"))` - Authentication endpoints
- `Description(x => x.WithTags("Orders"))` - Customer order endpoints
- `Description(x => x.WithTags("Products"))` - Public product browsing
- `Description(x => x.WithTags("Categories"))` - Public category browsing

When generating TypeScript clients with NSwag, these tags will create separate client classes:
- `AdminOrdersClient`, `AdminUsersClient`, `AdminProductsClient`, `AdminCategoriesClient`
- `AuthClient`, `OrdersClient`, `ProductsClient`, `CategoriesClient`

### Endpoint Without Request

```csharp
public class MyEndpoint : EndpointWithoutRequest<MyResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        // Implementation
    }
}
```

### Response Methods (CRITICAL)

**ALWAYS use `Send.*` methods, NOT the old methods:**

✅ **Correct**:
```csharp
await Send.OkAsync(response, ct);
await Send.CreatedAtAsync<OtherEndpoint>(new { id = 1 }, response, ct);
await Send.NotFoundAsync(ct);
await Send.UnauthorizedAsync(ct);
await Send.ForbiddenAsync(ct);
await Send.ErrorsAsync(ct);
```

❌ **Wrong** (old FastEndpoints syntax):
```csharp
await SendOkAsync(response, ct);           // Wrong
await SendCreatedAtAsync<...>(...)         // Wrong
await SendNotFoundAsync(ct);               // Wrong
await SendUnauthorizedAsync(ct);           // Wrong
```

### Validation

```csharp
public class MyRequestValidator : Validator<MyRequest>
{
    public MyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0");
    }
}
```

### Error Handling

```csharp
// Add validation errors
AddError("Error message");
await Send.ErrorsAsync(ct);

// Not found
await Send.NotFoundAsync(ct);

// Forbidden
await Send.ForbiddenAsync(ct);

// Unauthorized
await Send.UnauthorizedAsync(ct);
```

## Routing Conventions

### URL Naming

- Use **dashes** for multi-word segments: `/admin/users/admins`, `/admin/orders/update-status`
- Use **camelCase** for route parameters: `{id}`, `{productId}`, `{imageId}`
- Admin routes: `/admin/*`
- Auth routes: `/auth/*`
- Customer routes: `/orders`, `/profile`
- Public routes: `/products`, `/categories`

**Examples**:
```csharp
Get("/admin/products/{id}");                   // ✅ Correct
Get("/admin/products/{productId}/images");     // ✅ Correct
Put("/admin/products/images/{imageId}/set-main"); // ✅ Correct (dash in segment, camelCase param)

Get("/admin/products/{product_id}");           // ❌ Wrong (snake_case)
Get("/admin/products/{product-id}");           // ❌ Wrong (dash in parameter)
```

## Key Entities

### ApplicationUser
Extends IdentityUser with:
- `FirstName`, `LastName`, `Address`
- `CreatedAt`, `UpdatedAt`
- Navigation: `Orders`

### Product
- Has multiple `ProductImage` (removed single `ImageUrl`)
- One image marked as `IsMain = true`
- Belongs to `Category`
- Has `StockQuantity`, `IsActive`

### Order
- Belongs to `ApplicationUser` (via `UserId`)
- Has multiple `OrderItem`
- Status: Pending, Processing, Shipped, Delivered, Cancelled
- Tracks `OrderDate`, `ShippedDate`, `DeliveredDate`
- Auto-generates `OrderNumber` (format: `ORD-YYYYMMDD-XXXXXXXX`)

### OrderItem
- Links `Order` to `Product`
- Stores `Quantity`, `UnitPrice`, `TotalPrice`

## Common Patterns

### User Access Control

**Admin accessing any order**:
```csharp
var order = await _dbContext.Orders
    .Include(o => o.User)
    .FirstOrDefaultAsync(o => o.Id == req.Id, ct);
```

**Customer accessing own orders only**:
```csharp
var userId = User.FindFirst("sub")?.Value;
if (string.IsNullOrEmpty(userId))
{
    await Send.UnauthorizedAsync(ct);
    return;
}

var order = await _dbContext.Orders
    .Where(o => o.UserId == userId)
    .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

if (order == null)
{
    await Send.NotFoundAsync(ct);
    return;
}
```

### Product Queries

**With main image only** (for listings):
```csharp
var products = await _dbContext.Products
    .Include(p => p.Category)
    .Include(p => p.Images.Where(i => i.IsMain))
    .Where(p => p.IsActive)
    .ToListAsync(ct);
```

**With all images** (for details):
```csharp
var product = await _dbContext.Products
    .Include(p => p.Category)
    .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
    .FirstOrDefaultAsync(p => p.Id == id, ct);
```

### Order Processing

**Place order with stock validation**:
```csharp
// Validate products exist and are active
var products = await _dbContext.Products
    .Where(p => productIds.Contains(p.Id) && p.IsActive)
    .ToListAsync(ct);

// Check stock
if (product.StockQuantity < item.Quantity)
{
    AddError($"Insufficient stock for '{product.Name}'");
    await Send.ErrorsAsync(ct);
    return;
}

// Reduce stock
product.StockQuantity -= item.Quantity;

// Create order
var order = new Order { /* ... */ };
_dbContext.Orders.Add(order);
await _dbContext.SaveChangesAsync(ct);
```

## API Endpoints Summary

### Authentication
- `POST /auth/login` - Login with email/password, returns JWT + refresh token
- `POST /auth/refresh` - Refresh access token using refresh token

### Admin - Users
- `POST /admin/users/admins` - Create admin user
- `GET /admin/users/admins` - List all admins
- `GET /admin/users/admins/{id}` - Get admin details
- `PUT /admin/users/admins/{id}` - Update admin
- `DELETE /admin/users/admins/{id}` - Delete admin
- (Same pattern for `/admin/users/customers`)

### Admin - Categories
- `POST /admin/categories` - Create category
- `GET /admin/categories` - List all categories
- `GET /admin/categories/{id}` - Get category details
- `PUT /admin/categories/{id}` - Update category
- `DELETE /admin/categories/{id}` - Delete category

### Admin - Products
- `POST /admin/products` - Create product
- `GET /admin/products` - List all products with images
- `GET /admin/products/{id}` - Get product details
- `PUT /admin/products/{id}` - Update product
- `DELETE /admin/products/{id}` - Delete product
- `POST /admin/products/{id}/images` - Add image to product
- `DELETE /admin/products/images/{imageId}` - Remove image
- `PUT /admin/products/images/{imageId}/set-main` - Set main image

### Admin - Orders
- `GET /admin/orders` - List all orders
- `GET /admin/orders/{id}` - Get order details
- `PUT /admin/orders/{id}/status` - Update order status

### Customer - Orders
- `POST /orders` - Place new order (reduces stock)
- `GET /orders` - Get my orders
- `GET /orders/{id}` - Get my order details (user-isolated)

### Public
- `GET /categories` - List categories (minimal info)
- `GET /products` - List active products (with main image)
- `GET /products/{id}` - Get product details (all images)

## Important Notes

### Security
- Admin endpoints require `Roles(UserRoles.Admin)`
- Customer endpoints require authentication: `Roles(UserRoles.Admin, UserRoles.Customer)`
- Public endpoints use `AllowAnonymous()`
- User isolation: Customers can only access their own orders

### Database
- Use PostgreSQL 10.0.1 package version specifically
- Connection pooling configured (min: 1, max: 100)
- All timestamps use `DateTime.UtcNow`

### JWT Claims
- **CRITICAL**: Use `"role"` claim (short form), NOT `ClaimTypes.Role`
- Access user ID via `User.FindFirst("sub")?.Value`

### Code Style
- Use `Send.*` methods for responses
- camelCase for parameters, dashes for route segments
- Add comprehensive `Summary()` blocks to all endpoints
- Include FluentValidation for all requests with input

### Product Images
- Products must have at least one image
- Only one image can have `IsMain = true` per product
- Use `DisplayOrder` for image ordering
- Cascade delete: Deleting product deletes all images

### Orders
- Auto-generate unique `OrderNumber`
- Validate product availability and stock before creating
- Reduce `StockQuantity` when order is placed
- Auto-set `ShippedDate` when status changes to Shipped
- Auto-set `DeliveredDate` when status changes to Delivered

## Development Commands

```bash
# Run application
dotnet run

# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName

# Build
dotnet build

# Run tests (if any)
dotnet test
```

## Configuration

### appsettings.json Structure

```json
{
  "DatabaseConfig": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "ZaynaStore",
    "UserName": "postgres",
    "Password": "mysecretpassword",
    "MinimumPoolSize": 1,
    "MaximumPoolSize": 100
  },
  "DefaultAdmin": {
    "Email": "admin@zayna.com",
    "Password": "Admin@123"
  },
  "JwtSettings": {
    "SigningKey": "MyLongSecretForSigningJwtTokensMyLongSecretForSigningJwtTokens",
    "AccessTokenExpiryMinutes": 30,
    "RefreshTokenExpiryDays": 15,
    "Issuer": "ZaynaStore",
    "Audience": "ZaynaStoreClient"
  }
}
```

---

**Last Updated**: 2026-04-05
