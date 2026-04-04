using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Data;

public class DbSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly StoreDbContext _dbContext;

    public DbSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        StoreDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        // Seed Roles
        await SeedRolesAsync();

        // Seed Default Admin
        await SeedDefaultAdminAsync();

        // Seed Categories and Products
        await SeedCategoriesAndProductsAsync();
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

    private async Task SeedCategoriesAndProductsAsync()
    {
        // Check if categories already exist
        if (await _dbContext.Categories.AnyAsync())
        {
            return; // Already seeded
        }

        // Create Categories
        var footwear = new Category { Name = "Footwear", Description = "Shoes, sneakers, boots and more", CreatedAt = DateTime.UtcNow };
        var electronics = new Category { Name = "Electronics", Description = "Gadgets and electronic devices", CreatedAt = DateTime.UtcNow };
        var clothing = new Category { Name = "Clothing", Description = "Fashion apparel for all occasions", CreatedAt = DateTime.UtcNow };
        var sports = new Category { Name = "Sports & Fitness", Description = "Sports equipment and fitness gear", CreatedAt = DateTime.UtcNow };
        var home = new Category { Name = "Home & Living", Description = "Furniture and home decor", CreatedAt = DateTime.UtcNow };

        _dbContext.Categories.AddRange(footwear, electronics, clothing, sports, home);
        await _dbContext.SaveChangesAsync();

        var products = new List<Product>();

        // FOOTWEAR - 20 products
        products.AddRange(new[]
        {
            CreateProduct("Classic White Sneakers", "Timeless white leather sneakers perfect for everyday wear", 89.99m, 50, footwear.Id, "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=800&q=80"),
            CreateProduct("Running Performance Shoes", "Lightweight running shoes with superior cushioning", 129.99m, 45, footwear.Id, "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800&q=80"),
            CreateProduct("High-Top Basketball Sneakers", "Professional basketball shoes with ankle support", 149.99m, 30, footwear.Id, "https://images.unsplash.com/photo-1525966222134-fcfa99b8ae77?w=800&q=80"),
            CreateProduct("Leather Dress Shoes", "Premium leather oxford shoes for formal occasions", 179.99m, 25, footwear.Id, "https://images.unsplash.com/photo-1614252235316-8c857d38b5f4?w=800&q=80"),
            CreateProduct("Casual Canvas Shoes", "Comfortable canvas sneakers for casual outings", 49.99m, 60, footwear.Id, "https://images.unsplash.com/photo-1560769629-975ec94e6a86?w=800&q=80"),
            CreateProduct("Hiking Boots", "Durable waterproof hiking boots for outdoor adventures", 159.99m, 35, footwear.Id, "https://images.unsplash.com/photo-1608256246200-53e635b5b65f?w=800&q=80"),
            CreateProduct("Slip-On Loafers", "Stylish slip-on loafers for effortless style", 79.99m, 40, footwear.Id, "https://images.unsplash.com/photo-1533867617858-e7b97e060509?w=800&q=80"),
            CreateProduct("Athletic Training Shoes", "Versatile training shoes for gym workouts", 109.99m, 55, footwear.Id, "https://images.unsplash.com/photo-1460353581641-37baddab0fa2?w=800&q=80"),
            CreateProduct("Chelsea Boots", "Classic Chelsea boots in premium suede", 139.99m, 28, footwear.Id, "https://images.unsplash.com/photo-1638247025967-b4e38f787b76?w=800&q=80"),
            CreateProduct("Sandals", "Comfortable summer sandals with adjustable straps", 39.99m, 70, footwear.Id, "https://images.unsplash.com/photo-1603487742131-4160ec999306?w=800&q=80"),
            CreateProduct("Trail Running Shoes", "Rugged trail running shoes with excellent grip", 119.99m, 42, footwear.Id, "https://images.unsplash.com/photo-1551107696-a4b0c5a0d9a2?w=800&q=80"),
            CreateProduct("Formal Patent Leather Shoes", "Glossy patent leather shoes for special events", 169.99m, 20, footwear.Id, "https://images.unsplash.com/photo-1533867617858-e7b97e060509?w=800&q=80"),
            CreateProduct("Skate Shoes", "Durable skate shoes with reinforced toe caps", 69.99m, 48, footwear.Id, "https://images.unsplash.com/photo-1525966222134-fcfa99b8ae77?w=800&q=80"),
            CreateProduct("Winter Boots", "Insulated winter boots for cold weather", 189.99m, 32, footwear.Id, "https://images.unsplash.com/photo-1520639888713-7851133b1ed0?w=800&q=80"),
            CreateProduct("Espadrilles", "Lightweight espadrilles perfect for summer", 44.99m, 65, footwear.Id, "https://images.unsplash.com/photo-1543163521-1bf539c55dd2?w=800&q=80"),
            CreateProduct("Work Boots", "Steel-toe work boots for construction sites", 199.99m, 38, footwear.Id, "https://images.unsplash.com/photo-1605408499391-6368c628ef42?w=800&q=80"),
            CreateProduct("Ballet Flats", "Elegant ballet flats for everyday comfort", 59.99m, 52, footwear.Id, "https://images.unsplash.com/photo-1543163521-1bf539c55dd2?w=800&q=80"),
            CreateProduct("Boat Shoes", "Classic boat shoes with non-slip soles", 84.99m, 44, footwear.Id, "https://images.unsplash.com/photo-1560769629-975ec94e6a86?w=800&q=80"),
            CreateProduct("High Heels", "Sophisticated high heels for formal wear", 99.99m, 36, footwear.Id, "https://images.unsplash.com/photo-1543163521-1bf539c55dd2?w=800&q=80"),
            CreateProduct("Minimalist Sneakers", "Sleek minimalist design sneakers", 74.99m, 50, footwear.Id, "https://images.unsplash.com/photo-1600185365926-3a2ce3cdb9eb?w=800&q=80")
        });

        // ELECTRONICS - 20 products
        products.AddRange(new[]
        {
            CreateProduct("Wireless Bluetooth Headphones", "Premium noise-cancelling wireless headphones", 199.99m, 80, electronics.Id, "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&q=80"),
            CreateProduct("Smartphone 128GB", "Latest generation smartphone with advanced camera", 799.99m, 60, electronics.Id, "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&q=80"),
            CreateProduct("Laptop 15-inch", "High-performance laptop for professionals", 1299.99m, 45, electronics.Id, "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&q=80"),
            CreateProduct("Wireless Mouse", "Ergonomic wireless mouse with precision tracking", 29.99m, 120, electronics.Id, "https://images.unsplash.com/photo-1527814050087-3793815479db?w=800&q=80"),
            CreateProduct("Mechanical Keyboard", "RGB mechanical gaming keyboard", 89.99m, 75, electronics.Id, "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&q=80"),
            CreateProduct("4K Monitor 27-inch", "Ultra HD 4K monitor with HDR support", 349.99m, 50, electronics.Id, "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&q=80"),
            CreateProduct("Tablet 10-inch", "Portable tablet with stylus support", 449.99m, 65, electronics.Id, "https://images.unsplash.com/photo-1561154464-82e9adf32764?w=800&q=80"),
            CreateProduct("Smartwatch", "Fitness tracking smartwatch with GPS", 249.99m, 90, electronics.Id, "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&q=80"),
            CreateProduct("Wireless Earbuds", "True wireless earbuds with charging case", 129.99m, 100, electronics.Id, "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?w=800&q=80"),
            CreateProduct("Portable SSD 1TB", "Fast portable solid-state drive", 119.99m, 85, electronics.Id, "https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=800&q=80"),
            CreateProduct("Webcam 1080p", "Full HD webcam for video conferencing", 79.99m, 70, electronics.Id, "https://images.unsplash.com/photo-1587826080692-f439cd0b70da?w=800&q=80"),
            CreateProduct("USB-C Hub", "Multi-port USB-C hub with HDMI output", 49.99m, 95, electronics.Id, "https://images.unsplash.com/photo-1625948515291-69613efd103f?w=800&q=80"),
            CreateProduct("Gaming Console", "Next-gen gaming console with 4K graphics", 499.99m, 40, electronics.Id, "https://images.unsplash.com/photo-1606144042614-b2417e99c4e3?w=800&q=80"),
            CreateProduct("Digital Camera", "Professional mirrorless camera with 24MP sensor", 1499.99m, 30, electronics.Id, "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=800&q=80"),
            CreateProduct("Power Bank 20000mAh", "High-capacity portable charger", 39.99m, 110, electronics.Id, "https://images.unsplash.com/photo-1609091839311-d5365f9ff1c5?w=800&q=80"),
            CreateProduct("Bluetooth Speaker", "Waterproof portable Bluetooth speaker", 69.99m, 88, electronics.Id, "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=800&q=80"),
            CreateProduct("E-Reader", "E-ink display e-reader with adjustable backlight", 139.99m, 55, electronics.Id, "https://images.unsplash.com/photo-1592503254549-d83d24a4dfab?w=800&q=80"),
            CreateProduct("Drone with Camera", "4K camera drone with GPS navigation", 599.99m, 35, electronics.Id, "https://images.unsplash.com/photo-1473968512647-3e447244af8f?w=800&q=80"),
            CreateProduct("Smart Home Hub", "Voice-controlled smart home assistant", 99.99m, 75, electronics.Id, "https://images.unsplash.com/photo-1558089687-e1038e1c2eeb?w=800&q=80"),
            CreateProduct("VR Headset", "Immersive virtual reality headset", 399.99m, 42, electronics.Id, "https://images.unsplash.com/photo-1622979135225-d2ba269cf1ac?w=800&q=80")
        });

        // CLOTHING - 20 products
        products.AddRange(new[]
        {
            CreateProduct("Classic Denim Jeans", "Comfortable straight-fit denim jeans", 59.99m, 100, clothing.Id, "https://images.unsplash.com/photo-1542272604-787c3835535d?w=800&q=80"),
            CreateProduct("Cotton T-Shirt White", "Premium cotton crew neck t-shirt", 24.99m, 150, clothing.Id, "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=800&q=80"),
            CreateProduct("Leather Jacket", "Genuine leather biker jacket", 299.99m, 40, clothing.Id, "https://images.unsplash.com/photo-1551028719-00167b16eac5?w=800&q=80"),
            CreateProduct("Wool Sweater", "Cozy merino wool pullover sweater", 79.99m, 65, clothing.Id, "https://images.unsplash.com/photo-1576566588028-4147f3842f27?w=800&q=80"),
            CreateProduct("Formal Dress Shirt", "Crisp white cotton dress shirt", 49.99m, 85, clothing.Id, "https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=800&q=80"),
            CreateProduct("Summer Dress", "Floral print summer maxi dress", 69.99m, 70, clothing.Id, "https://images.unsplash.com/photo-1595777457583-95e059d581b8?w=800&q=80"),
            CreateProduct("Hooded Sweatshirt", "Comfortable cotton blend hoodie", 54.99m, 95, clothing.Id, "https://images.unsplash.com/photo-1556821840-3a63f95609a7?w=800&q=80"),
            CreateProduct("Chino Pants", "Slim-fit chino pants in khaki", 64.99m, 80, clothing.Id, "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?w=800&q=80"),
            CreateProduct("Blazer", "Tailored business blazer in navy", 159.99m, 50, clothing.Id, "https://images.unsplash.com/photo-1507679799987-c73779587ccf?w=800&q=80"),
            CreateProduct("Polo Shirt", "Classic pique polo shirt", 39.99m, 110, clothing.Id, "https://images.unsplash.com/photo-1607345366928-199ea26cfe3e?w=800&q=80"),
            CreateProduct("Winter Coat", "Insulated parka for cold weather", 199.99m, 45, clothing.Id, "https://images.unsplash.com/photo-1539533113208-f6df8cc8b543?w=800&q=80"),
            CreateProduct("Yoga Pants", "Stretchy high-waisted yoga leggings", 44.99m, 120, clothing.Id, "https://images.unsplash.com/photo-1506629082955-511b1aa562c8?w=800&q=80"),
            CreateProduct("Cardigan Sweater", "Soft knit button-up cardigan", 59.99m, 75, clothing.Id, "https://images.unsplash.com/photo-1583743814966-8936f5b7be1a?w=800&q=80"),
            CreateProduct("Denim Jacket", "Vintage-style denim trucker jacket", 89.99m, 60, clothing.Id, "https://images.unsplash.com/photo-1523205771623-e0faa4d2813d?w=800&q=80"),
            CreateProduct("Cocktail Dress", "Elegant evening cocktail dress", 129.99m, 35, clothing.Id, "https://images.unsplash.com/photo-1566174053879-31528523f8ae?w=800&q=80"),
            CreateProduct("Track Pants", "Athletic track pants with side stripes", 49.99m, 90, clothing.Id, "https://images.unsplash.com/photo-1506629082955-511b1aa562c8?w=800&q=80"),
            CreateProduct("Button-Down Shirt", "Casual plaid flannel shirt", 44.99m, 85, clothing.Id, "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800&q=80"),
            CreateProduct("Shorts", "Casual summer shorts with pockets", 34.99m, 100, clothing.Id, "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?w=800&q=80"),
            CreateProduct("Trench Coat", "Classic beige trench coat", 179.99m, 38, clothing.Id, "https://images.unsplash.com/photo-1539533113208-f6df8cc8b543?w=800&q=80"),
            CreateProduct("Graphic T-Shirt", "Trendy graphic print t-shirt", 29.99m, 125, clothing.Id, "https://images.unsplash.com/photo-1583743814966-8936f5b7be1a?w=800&q=80")
        });

        // SPORTS & FITNESS - 20 products
        products.AddRange(new[]
        {
            CreateProduct("Yoga Mat", "Non-slip yoga mat with carrying strap", 29.99m, 150, sports.Id, "https://images.unsplash.com/photo-1601925260368-ae2f83cf8b7f?w=800&q=80"),
            CreateProduct("Dumbbell Set", "Adjustable dumbbell set 5-50 lbs", 199.99m, 50, sports.Id, "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=800&q=80"),
            CreateProduct("Resistance Bands", "Set of 5 resistance bands with handles", 24.99m, 180, sports.Id, "https://images.unsplash.com/photo-1598632640487-6ea4a4e8b928?w=800&q=80"),
            CreateProduct("Basketball", "Official size basketball for indoor/outdoor", 34.99m, 90, sports.Id, "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=800&q=80"),
            CreateProduct("Football", "Premium leather football", 39.99m, 85, sports.Id, "https://images.unsplash.com/photo-1560272564-c83b66b1ad12?w=800&q=80"),
            CreateProduct("Tennis Racket", "Professional carbon fiber tennis racket", 149.99m, 45, sports.Id, "https://images.unsplash.com/photo-1622163642998-1ea32b0bbc67?w=800&q=80"),
            CreateProduct("Jump Rope", "Speed jump rope for cardio workouts", 14.99m, 200, sports.Id, "https://images.unsplash.com/photo-1601925260368-ae2f83cf8b7f?w=800&q=80"),
            CreateProduct("Gym Bag", "Spacious duffel bag for gym essentials", 44.99m, 110, sports.Id, "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800&q=80"),
            CreateProduct("Foam Roller", "High-density foam roller for muscle recovery", 29.99m, 140, sports.Id, "https://images.unsplash.com/photo-1598632640487-6ea4a4e8b928?w=800&q=80"),
            CreateProduct("Kettlebell 20kg", "Cast iron kettlebell for strength training", 59.99m, 70, sports.Id, "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=800&q=80"),
            CreateProduct("Exercise Ball", "Anti-burst stability ball 65cm", 24.99m, 130, sports.Id, "https://images.unsplash.com/photo-1598289431512-b97b0917affc?w=800&q=80"),
            CreateProduct("Boxing Gloves", "Professional boxing gloves 12oz", 79.99m, 65, sports.Id, "https://images.unsplash.com/photo-1549719386-74dfcbf7dbed?w=800&q=80"),
            CreateProduct("Bicycle Helmet", "Lightweight cycling helmet with ventilation", 49.99m, 95, sports.Id, "https://images.unsplash.com/photo-1557224326-968f0f536c07?w=800&q=80"),
            CreateProduct("Water Bottle 1L", "Insulated stainless steel water bottle", 19.99m, 220, sports.Id, "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800&q=80"),
            CreateProduct("Running Belt", "Waterproof running belt for phone and keys", 16.99m, 160, sports.Id, "https://images.unsplash.com/photo-1571902943202-507ec2618e8f?w=800&q=80"),
            CreateProduct("Ankle Weights", "Adjustable ankle weights 2 lbs each", 34.99m, 100, sports.Id, "https://images.unsplash.com/photo-1598632640487-6ea4a4e8b928?w=800&q=80"),
            CreateProduct("Pull-Up Bar", "Doorway pull-up bar with multiple grips", 39.99m, 80, sports.Id, "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=800&q=80"),
            CreateProduct("Ab Wheel", "Core strengthening ab roller wheel", 19.99m, 150, sports.Id, "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=800&q=80"),
            CreateProduct("Yoga Block Set", "Cork yoga blocks set of 2", 24.99m, 125, sports.Id, "https://images.unsplash.com/photo-1601925260368-ae2f83cf8b7f?w=800&q=80"),
            CreateProduct("Soccer Ball", "Official size 5 soccer ball", 29.99m, 105, sports.Id, "https://images.unsplash.com/photo-1614632537423-1e6c2e7e0aab?w=800&q=80")
        });

        // HOME & LIVING - 20 products
        products.AddRange(new[]
        {
            CreateProduct("Modern Floor Lamp", "Minimalist LED floor lamp with dimmer", 89.99m, 60, home.Id, "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=800&q=80"),
            CreateProduct("Throw Pillows Set", "Decorative throw pillows set of 4", 49.99m, 100, home.Id, "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=800&q=80"),
            CreateProduct("Wall Clock", "Vintage-style wall clock", 39.99m, 85, home.Id, "https://images.unsplash.com/photo-1563861826100-9cb868fdbe1c?w=800&q=80"),
            CreateProduct("Area Rug 5x7", "Soft plush area rug for living room", 129.99m, 45, home.Id, "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=800&q=80"),
            CreateProduct("Coffee Table", "Modern wooden coffee table with storage", 249.99m, 30, home.Id, "https://images.unsplash.com/photo-1532372320572-cda25653a26d?w=800&q=80"),
            CreateProduct("Bed Sheets Queen", "Egyptian cotton bed sheet set", 79.99m, 75, home.Id, "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=800&q=80"),
            CreateProduct("Curtains 84-inch", "Blackout curtains panel pair", 54.99m, 90, home.Id, "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?w=800&q=80"),
            CreateProduct("Picture Frame Set", "Gallery wall picture frames set of 7", 44.99m, 110, home.Id, "https://images.unsplash.com/photo-1513519245088-0e12902e35ca?w=800&q=80"),
            CreateProduct("Desk Organizer", "Bamboo desk organizer with compartments", 34.99m, 120, home.Id, "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&q=80"),
            CreateProduct("Planter Pot", "Ceramic planter pot with drainage", 24.99m, 140, home.Id, "https://images.unsplash.com/photo-1485955900006-10f4d324d411?w=800&q=80"),
            CreateProduct("Candle Set", "Scented soy candles set of 3", 29.99m, 160, home.Id, "https://images.unsplash.com/photo-1602874801006-e157a9a40f55?w=800&q=80"),
            CreateProduct("Storage Basket", "Woven storage basket with handles", 39.99m, 95, home.Id, "https://images.unsplash.com/photo-1566328386401-b2980125f6b5?w=800&q=80"),
            CreateProduct("Dining Chair", "Mid-century modern dining chair", 149.99m, 40, home.Id, "https://images.unsplash.com/photo-1503602642458-232111445657?w=800&q=80"),
            CreateProduct("Wall Mirror", "Large decorative wall mirror", 99.99m, 50, home.Id, "https://images.unsplash.com/photo-1618220179428-22790b461013?w=800&q=80"),
            CreateProduct("Bookshelf", "5-tier ladder bookshelf", 179.99m, 35, home.Id, "https://images.unsplash.com/photo-1594620302200-9a762244a156?w=800&q=80"),
            CreateProduct("Table Lamp", "Ceramic table lamp with fabric shade", 59.99m, 80, home.Id, "https://images.unsplash.com/photo-1560448204-603b3fc33ddc?w=800&q=80"),
            CreateProduct("Bath Towels Set", "Luxury bath towels set of 6", 64.99m, 100, home.Id, "https://images.unsplash.com/photo-1584556326561-c8746083993b?w=800&q=80"),
            CreateProduct("Vase", "Hand-blown glass vase", 44.99m, 70, home.Id, "https://images.unsplash.com/photo-1578500494198-246f612d3b3d?w=800&q=80"),
            CreateProduct("Wall Art Canvas", "Abstract wall art canvas print", 89.99m, 65, home.Id, "https://images.unsplash.com/photo-1513519245088-0e12902e35ca?w=800&q=80"),
            CreateProduct("Ottoman", "Upholstered storage ottoman", 119.99m, 42, home.Id, "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=800&q=80")
        });

        _dbContext.Products.AddRange(products);
        await _dbContext.SaveChangesAsync();
    }

    private Product CreateProduct(string name, string description, decimal price, int stock, int categoryId, string imageUrl)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = stock,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        product.Images.Add(new ProductImage
        {
            ImageUrl = imageUrl,
            IsMain = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow
        });

        return product;
    }
}
