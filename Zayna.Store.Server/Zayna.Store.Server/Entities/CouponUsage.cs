namespace Zayna.Store.Server.Entities;

public class CouponUsage
{
    public int Id { get; set; }
    public int CouponId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Coupon Coupon { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
