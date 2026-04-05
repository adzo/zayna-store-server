namespace Zayna.Store.Server.Entities;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int? MaxUses { get; set; } // null = unlimited
    public int CurrentUses { get; set; } = 0;
    public decimal? MinimumOrderAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public enum DiscountType
{
    Percentage,
    FixedAmount
}
