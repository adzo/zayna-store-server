# Zayna Store Server

A modern e-commerce REST API built with ASP.NET Core 9.0 and FastEndpoints, featuring JWT authentication, role-based authorization, and comprehensive order management.

## Features

- 🔐 **JWT Authentication** - Secure token-based authentication with refresh tokens
- 👥 **User Management** - Admin and customer user roles with role-based access control
- 🛍️ **Product Catalog** - Full product management with multiple images per product
- 📦 **Categories** - Hierarchical category system for product organization
- 🛒 **Order Processing** - Complete order lifecycle with stock management
- 📸 **Image Management** - Multiple product images with main image designation
- 🔒 **Role-Based Authorization** - Separate endpoints for admin and customer operations
- 🌐 **Public API** - Anonymous access to browse products and categories
- 📚 **API Documentation** - Interactive Swagger/OpenAPI documentation

## Tech Stack

- **Framework**: ASP.NET Core 9.0
- **Endpoint Framework**: FastEndpoints 8.1.0
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10.0
- **Database Provider**: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1
- **Authentication**: ASP.NET Core Identity + JWT Bearer
- **Validation**: FluentValidation (via FastEndpoints)
- **Documentation**: Swagger/OpenAPI (via FastEndpoints)

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- [Entity Framework Core CLI](https://docs.microsoft.com/en-us/ef/core/cli/dotnet)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd zayna-store-server/Zayna.Store.Server
```

### 2. Configure Database

Update the database configuration in `appsettings.json`:

```json
{
  "DatabaseConfig": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "ZaynaStore",
    "UserName": "postgres",
    "Password": "your-password",
    "MinimumPoolSize": 1,
    "MaximumPoolSize": 100
  }
}
```

### 3. Configure JWT Settings

Update JWT configuration in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SigningKey": "your-secret-key-minimum-32-characters-long",
    "AccessTokenExpiryMinutes": 30,
    "RefreshTokenExpiryDays": 15,
    "Issuer": "ZaynaStore",
    "Audience": "ZaynaStoreClient"
  }
}
```

### 4. Run Database Migrations

```bash
dotnet ef database update
```

This will create the database and seed initial data:
- Default admin user: `admin@zayna.com` / `Admin@123`
- Sample categories and products

### 5. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### Authentication

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/auth/login` | Login with email/password | Public |
| POST | `/auth/refresh` | Refresh access token | Public |

### Public Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/categories` | List all categories | Public |
| GET | `/products` | List active products | Public |
| GET | `/products/{id}` | Get product details | Public |

### Customer Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/orders` | Place a new order | Customer |
| GET | `/orders` | Get my orders | Customer |
| GET | `/orders/{id}` | Get my order details | Customer |

### Admin - User Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/admin/users/admins` | Create admin user | Admin |
| GET | `/admin/users/admins` | List all admins | Admin |
| GET | `/admin/users/admins/{id}` | Get admin details | Admin |
| PUT | `/admin/users/admins/{id}` | Update admin | Admin |
| DELETE | `/admin/users/admins/{id}` | Delete admin | Admin |
| POST | `/admin/users/customers` | Create customer user | Admin |
| GET | `/admin/users/customers` | List all customers | Admin |
| GET | `/admin/users/customers/{id}` | Get customer details | Admin |
| PUT | `/admin/users/customers/{id}` | Update customer | Admin |
| DELETE | `/admin/users/customers/{id}` | Delete customer | Admin |

### Admin - Category Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/admin/categories` | Create category | Admin |
| GET | `/admin/categories` | List all categories | Admin |
| GET | `/admin/categories/{id}` | Get category details | Admin |
| PUT | `/admin/categories/{id}` | Update category | Admin |
| DELETE | `/admin/categories/{id}` | Delete category | Admin |

### Admin - Product Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/admin/products` | Create product | Admin |
| GET | `/admin/products` | List all products | Admin |
| GET | `/admin/products/{id}` | Get product details | Admin |
| PUT | `/admin/products/{id}` | Update product | Admin |
| DELETE | `/admin/products/{id}` | Delete product | Admin |
| POST | `/admin/products/{id}/images` | Add image to product | Admin |
| DELETE | `/admin/products/images/{imageId}` | Remove product image | Admin |
| PUT | `/admin/products/images/{imageId}/set-main` | Set main image | Admin |

### Admin - Order Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/admin/orders` | List all orders | Admin |
| GET | `/admin/orders/{id}` | Get order details | Admin |
| PUT | `/admin/orders/{id}/status` | Update order status | Admin |

## Authentication

### Login

Send credentials to `/auth/login`:

```json
{
  "email": "admin@zayna.com",
  "password": "Admin@123"
}
```

Response:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "xxxx-xxxx-xxxx-xxxx",
  "expiresAt": "2026-04-05T12:30:00Z"
}
```

### Using the Token

Include the access token in the Authorization header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Refreshing Token

When the access token expires, use the refresh token:

```json
{
  "refreshToken": "xxxx-xxxx-xxxx-xxxx"
}
```

## Order Processing

### Place Order

Customer endpoint to create a new order:

```json
{
  "shippingAddress": "123 Main St, City, Country",
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 5,
      "quantity": 1
    }
  ]
}
```

Features:
- Validates product availability
- Checks stock quantity
- Automatically reduces stock
- Generates unique order number
- Calculates total price

### Order Statuses

- `Pending` - Order placed, awaiting processing
- `Processing` - Order is being prepared
- `Shipped` - Order has been shipped (auto-sets ShippedDate)
- `Delivered` - Order delivered (auto-sets DeliveredDate)
- `Cancelled` - Order cancelled

## Product Management

### Multiple Images

Products support multiple images:
- Each product can have multiple images
- One image must be designated as the main image (`IsMain = true`)
- Images have a `DisplayOrder` for sorting
- Deleting a product cascades to delete all images

### Stock Management

- Products have `StockQuantity` field
- Stock is automatically reduced when orders are placed
- Orders fail if insufficient stock

## Development

### Database Migrations

Create a new migration:

```bash
dotnet ef migrations add MigrationName
```

Apply migrations:

```bash
dotnet ef database update
```

Rollback to a previous migration:

```bash
dotnet ef database update PreviousMigrationName
```

### Project Structure

```
Zayna.Store.Server/
├── Data/                  # Database context and seeding
├── Entities/              # Domain models
├── Features/              # Endpoints organized by feature
│   ├── Admin/            # Admin-only endpoints
│   ├── Auth/             # Authentication
│   ├── Orders/           # Customer orders
│   └── Public/           # Public browsing
├── Services/             # Business logic services
└── Program.cs            # Application startup
```

### Code Conventions

- **Route naming**: Use dashes for multi-word segments, camelCase for parameters
- **Authorization**: Use `Roles()` for role-based access control
- **Validation**: FluentValidation for all request DTOs
- **Responses**: Use `Send.*` methods (e.g., `Send.OkAsync()`)
- **Swagger tags**: All endpoints must have `Description(x => x.WithTags("..."))`

## Configuration

### Environment Variables

You can override configuration using environment variables:

```bash
export DatabaseConfig__Host=your-db-host
export DatabaseConfig__Password=your-password
export JwtSettings__SigningKey=your-secret-key
```

### Default Admin Account

On first run, a default admin account is created:
- **Email**: `admin@zayna.com`
- **Password**: `Admin@123`

⚠️ **Change this password in production!**

## API Documentation

Interactive API documentation is available via Swagger UI:

- Development: `https://localhost:5001/swagger`
- Production: `https://your-domain.com/swagger`

## Security Features

- Password hashing with ASP.NET Core Identity
- JWT with short-lived access tokens (30 minutes)
- Refresh token rotation (15 days)
- Role-based authorization (Admin, Customer)
- User isolation (customers can only access their own orders)
- Input validation with FluentValidation
- Protection against common vulnerabilities (SQL injection, XSS)

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]

## Support

[Add support information here]
