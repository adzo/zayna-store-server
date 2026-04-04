using Microsoft.AspNetCore.Identity;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Data;

public class DbSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public DbSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        // Seed Roles
        await SeedRolesAsync();

        // Seed Default Admin
        await SeedDefaultAdminAsync();
    }

    private async Task SeedRolesAsync()
    {
        if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
        {
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
        }

        if (!await _roleManager.RoleExistsAsync(UserRoles.Customer))
        {
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.Customer));
        }
    }

    private async Task SeedDefaultAdminAsync()
    {
        var adminEmail = _configuration["DefaultAdmin:Email"] ?? "admin@zayna.com";
        var adminPassword = _configuration["DefaultAdmin:Password"] ?? "Admin@123";

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                PhoneNumber = "0000000000",
                Address = "System",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(admin, adminPassword);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, UserRoles.Admin);
            }
        }
    }
}
