using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Orders;

public class GetAllOrdersResponse
{
    public List<OrderDto> Orders { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public int ItemCount { get; set; }
}

public class GetAllOrdersEndpoint : EndpointWithoutRequest<GetAllOrdersResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetAllOrdersEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/orders");
        Roles(UserRoles.Admin);

        Summary(s =>
        {
            s.Summary = "Retrieves all orders";
            s.Description = "Returns a list of all orders with customer information and order details. Only accessible by administrators.";
            s.Response<GetAllOrdersResponse>(StatusCodes.Status200OK, "List of orders retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orders = await _dbContext.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserId = o.UserId,
                CustomerName = $"{o.User.FirstName} {o.User.LastName}",
                CustomerEmail = o.User.Email!,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ShippingAddress = o.ShippingAddress,
                ShippedDate = o.ShippedDate,
                DeliveredDate = o.DeliveredDate,
                ItemCount = o.OrderItems.Count
            })
            .ToListAsync(ct);

        await Send.OkAsync(new GetAllOrdersResponse
        {
            Orders = orders
        }, ct);
    }
}
