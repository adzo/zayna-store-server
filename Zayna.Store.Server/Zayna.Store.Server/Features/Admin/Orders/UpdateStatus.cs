using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Features.Admin.Orders;

public class UpdateOrderStatusRequest
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
}

public class UpdateOrderStatusResponse
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
}

public class UpdateOrderStatusValidator : Validator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Order ID is required");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid order status");
    }
}

public class UpdateOrderStatusEndpoint : Endpoint<UpdateOrderStatusRequest, UpdateOrderStatusResponse>
{
    private readonly StoreDbContext _dbContext;

    public UpdateOrderStatusEndpoint(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Put("/admin/orders/{id}/status");
        Roles(UserRoles.Admin);

        Summary(s =>
        {
            s.Summary = "Updates order status";
            s.Description = "Changes the status of an order. Automatically sets ShippedDate when status is Shipped and DeliveredDate when status is Delivered. Only accessible by administrators.";
            s.Response<UpdateOrderStatusResponse>(StatusCodes.Status200OK, "Order status updated successfully");
            s.Response<ProblemDetails>(StatusCodes.Status400BadRequest, "Invalid request or status");
            s.Response<ProblemDetails>(StatusCodes.Status401Unauthorized, "Unauthorized - authentication required");
            s.Response<ProblemDetails>(StatusCodes.Status403Forbidden, "Forbidden - Admin role required");
            s.Response<ProblemDetails>(StatusCodes.Status404NotFound, "Order not found");
        });
    }

    public override async Task HandleAsync(UpdateOrderStatusRequest req, CancellationToken ct)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

        if (order == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        order.Status = req.Status;

        // Automatically set dates based on status
        if (req.Status == OrderStatus.Shipped && order.ShippedDate == null)
        {
            order.ShippedDate = DateTime.UtcNow;
        }

        if (req.Status == OrderStatus.Delivered && order.DeliveredDate == null)
        {
            order.DeliveredDate = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);

        var response = new UpdateOrderStatusResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            ShippedDate = order.ShippedDate,
            DeliveredDate = order.DeliveredDate
        };

        await Send.OkAsync(response, ct);
    }
}
