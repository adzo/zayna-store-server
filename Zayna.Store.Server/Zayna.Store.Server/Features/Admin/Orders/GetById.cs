using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Orders;

public class GetOrderByIdRequest
{
    public int Id { get; set; }
}

public class GetOrderByIdResponse
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CouponCode { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class GetOrderByIdEndpoint : Endpoint<GetOrderByIdRequest, GetOrderByIdResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetOrderByIdEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/orders/{id}");
        Roles(UserRoles.Admin);
        Description(x => x.WithTags("AdminOrders"));

        Summary(s =>
        {
            s.Summary = "Retrieves order details by ID";
            s.Description = "Returns full order details including customer information and order items. Only accessible by administrators.";
            s.Response<GetOrderByIdResponse>(StatusCodes.Status200OK, "Order retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Order not found");
        });
    }

    public override async Task HandleAsync(GetOrderByIdRequest req, CancellationToken ct)
    {
        var order = await _dbContext.Orders
            .Include(o => o.User)
            .Include(o => o.Coupon)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

        if (order == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var response = new GetOrderByIdResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = $"{order.User.FirstName} {order.User.LastName}",
            CustomerEmail = order.User.Email!,
            CustomerPhone = order.User.PhoneNumber ?? string.Empty,
            OrderDate = order.OrderDate,
            Status = order.Status,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            CouponCode = order.Coupon?.Code,
            ShippingAddress = order.ShippingAddress,
            ShippedDate = order.ShippedDate,
            DeliveredDate = order.DeliveredDate,
            OrderItems = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product.Name,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice
            }).ToList()
        };

        await Send.OkAsync(response, ct);
    }
}
