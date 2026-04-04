using Microsoft.AspNetCore.Identity;

namespace Zayna.Store.Server.Entities;

public class ApplicationUser: IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public override string? PhoneNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}