using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Orders;

public class GetAllMyOrdersResponse
{
    public List<MyOrderDto> Orders { get; set; } = new();
}

public class MyOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public int ItemCount { get; set; }
}

public class GetAllMyOrdersEndpoint : EndpointWithoutRequest<GetAllMyOrdersResponse>
{
    private readonly StoreDbContext _dbContext;

    public GetAllMyOrdersEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/orders");
        Roles(UserRoles.Admin, UserRoles.Customer);
        Tags("Orders");
        Description(x => x.WithTags("Orders"));

        Summary(s =>
        {
            s.Summary = "Retrieves all orders for authenticated user";
            s.Description = "Returns a list of all orders placed by the authenticated user. Only shows orders belonging to the current user.";
            s.Response<GetAllMyOrdersResponse>(StatusCodes.Status200OK, "List of orders retrieved successfully");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var orders = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new MyOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ShippingAddress = o.ShippingAddress,
                ShippedDate = o.ShippedDate,
                DeliveredDate = o.DeliveredDate,
                ItemCount = o.OrderItems.Count
            })
            .ToListAsync(ct);

        await Send.OkAsync(new GetAllMyOrdersResponse
        {
            Orders = orders
        }, ct);
    }
}
