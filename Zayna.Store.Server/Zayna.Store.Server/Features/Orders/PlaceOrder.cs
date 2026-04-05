using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;
using Order = Zayna.Store.Server.Entities.Order;

namespace Zayna.Store.Server.Features.Orders;

public class PlaceOrderRequest
{
    public string ShippingAddress { get; set; } = string.Empty;
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class PlaceOrderResponse
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public List<PlacedOrderItemDto> Items { get; set; } = new();
}

public class PlacedOrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PlaceOrderValidator : Validator<PlaceOrderRequest>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotEmpty()
            .WithMessage("Shipping address is required")
            .MaximumLength(500)
            .WithMessage("Shipping address cannot exceed 500 characters");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .WithMessage("Valid product ID is required");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than 0");
        });
    }
}

public class PlaceOrderEndpoint : Endpoint<PlaceOrderRequest, PlaceOrderResponse>
{
    private readonly StoreDbContext _dbContext;

    public PlaceOrderEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/orders");
        Roles(UserRoles.Admin, UserRoles.Customer);

        Summary(s =>
        {
            s.Summary = "Place a new order";
            s.Description = "Creates a new order for the authenticated user. Validates product availability and calculates total amount. Accessible by both customers and admins.";
            s.Response<PlaceOrderResponse>(StatusCodes.Status201Created, "Order placed successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request - missing items, insufficient stock, or invalid products");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
        });
    }

    public override async Task HandleAsync(PlaceOrderRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Fetch products and validate availability
        var productIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync(ct);

        if (products.Count != productIds.Count)
        {
            AddError("One or more products are not available");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Validate stock and calculate total
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var item in req.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);

            if (product.StockQuantity < item.Quantity)
            {
                AddError($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            var itemTotal = product.Price * item.Quantity;
            totalAmount += itemTotal;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                TotalPrice = itemTotal
            });

            // Reduce stock
            product.StockQuantity -= item.Quantity;
        }

        // Generate order number
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // Create order
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            ShippingAddress = req.ShippingAddress,
            OrderItems = orderItems
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        var response = new PlaceOrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            Items = order.OrderItems.Select(oi => new PlacedOrderItemDto
            {
                ProductId = oi.ProductId,
                ProductName = products.First(p => p.Id == oi.ProductId).Name,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice
            }).ToList()
        };

        await Send.CreatedAtAsync<GetMyOrderByIdEndpoint>(new { id = order.Id }, response, cancellation: ct);
    }
}
