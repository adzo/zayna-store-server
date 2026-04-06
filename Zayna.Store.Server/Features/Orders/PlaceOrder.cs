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
    public string? CouponCode { get; set; }
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
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CouponCode { get; set; }
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
        Tags("Orders");
        Description(x => x.WithTags("Orders"));

        Summary(s =>
        {
            s.Summary = "Place a new order";
            s.Description = "Creates a new order for the authenticated user. Validates product availability, applies coupon discount if provided, and calculates total amount. Accessible by both customers and admins.";
            s.Response<PlaceOrderResponse>(StatusCodes.Status201Created, "Order placed successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request - missing items, insufficient stock, invalid products, or invalid coupon");
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

        // Validate stock and calculate subtotal
        var orderItems = new List<OrderItem>();
        decimal subTotal = 0;

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
            subTotal += itemTotal;

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

        // Validate and apply coupon if provided
        Coupon? coupon = null;
        decimal discountAmount = 0;

        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            coupon = await _dbContext.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == req.CouponCode.ToLower(), ct);

            if (coupon == null)
            {
                AddError("Coupon code does not exist");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (!coupon.IsActive)
            {
                AddError("This coupon is no longer active");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            var now = DateTime.UtcNow;
            if (now < coupon.ValidFrom || now > coupon.ValidTo)
            {
                AddError("This coupon is not valid at this time");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            var alreadyUsed = await _dbContext.CouponUsages
                .AnyAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId, ct);

            if (alreadyUsed)
            {
                AddError("You have already used this coupon");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (coupon.MaxUses.HasValue && coupon.CurrentUses >= coupon.MaxUses.Value)
            {
                AddError("This coupon has reached its maximum number of uses");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (coupon.MinimumOrderAmount.HasValue && subTotal < coupon.MinimumOrderAmount.Value)
            {
                AddError($"Minimum order amount of {coupon.MinimumOrderAmount.Value:C} required to use this coupon");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            // Calculate discount
            discountAmount = coupon.DiscountType == DiscountType.Percentage
                ? subTotal * (coupon.DiscountValue / 100)
                : coupon.DiscountValue;

            if (discountAmount > subTotal)
            {
                discountAmount = subTotal;
            }
        }

        decimal totalAmount = subTotal - discountAmount;

        // Generate order number
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // Create order
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            CouponId = coupon?.Id,
            ShippingAddress = req.ShippingAddress,
            OrderItems = orderItems
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        // Create coupon usage record if coupon was applied
        if (coupon != null)
        {
            var couponUsage = new CouponUsage
            {
                CouponId = coupon.Id,
                UserId = userId,
                OrderId = order.Id,
                UsedAt = DateTime.UtcNow
            };

            _dbContext.CouponUsages.Add(couponUsage);

            // Increment coupon usage count
            coupon.CurrentUses++;

            await _dbContext.SaveChangesAsync(ct);
        }

        var response = new PlaceOrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            CouponCode = coupon?.Code,
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
