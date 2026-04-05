using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Orders;

public class GetMyOrderByIdRequest
{
    public int Id { get; set; }
}

public class GetMyOrderByIdResponse
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
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public List<MyOrderItemDto> OrderItems { get; set; } = new();
}

public class MyOrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class GetMyOrderByIdEndpoint : Endpoint<GetMyOrderByIdRequest, GetMyOrderByIdResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetMyOrderByIdEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/orders/{id}");
        Roles(UserRoles.Admin, UserRoles.Customer);
        Description(x => x.WithTags("Orders"));

        Summary(s =>
        {
            s.Summary = "Retrieves order details by ID";
            s.Description = "Returns full order details including order items. Users can only access their own orders.";
            s.Response<GetMyOrderByIdResponse>(StatusCodes.Status200OK, "Order retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - you can only access your own orders");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Order not found");
        });
    }

    public override async Task HandleAsync(GetMyOrderByIdRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var order = await _dbContext.Orders
            .Include(o => o.Coupon)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

        if (order == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Verify the order belongs to the authenticated user
        if (order.UserId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var response = new GetMyOrderByIdResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            CouponCode = order.Coupon?.Code,
            ShippingAddress = order.ShippingAddress,
            ShippedDate = order.ShippedDate,
            DeliveredDate = order.DeliveredDate,
            OrderItems = order.OrderItems.Select(oi => new MyOrderItemDto
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
