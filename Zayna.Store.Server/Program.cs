using System.Text;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;
using Zayna.Store.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Configure Database
var databaseConfig = builder.Configuration.GetSection("DatabaseConfig").Get<DatabaseConfig>()
    ?? throw new InvalidOperationException("DatabaseConfig section is missing or invalid in appsettings.json");

builder.Services.AddSingleton(databaseConfig);

builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseNpgsql(databaseConfig.ConnectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<StoreDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var secret = builder.Configuration.GetSection("JwtSettings:SigningKey").Get<string>()
                     ?? throw new ApplicationException("Invalid signing key");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration.GetSection("JwtSettings:Issuer").Get<string>(),
            ValidAudience = builder.Configuration.GetSection("JwtSettings:Audience").Get<string>(),
            IssuerSigningKey = key,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.AutoTagPathSegmentIndex = 0;
        o.DocumentSettings = s =>
        {
            s.Title = "Zayna Store";
            s.Description = "Zayna Store API for our new Store";
            s.Version = "v1";

            // s.AddAuth("Bearer", new OpenApiSecurityScheme
            // {
            //     Type = OpenApiSecuritySchemeType.Http,
            //     Scheme = "bearer",
            //     BearerFormat = "JWT",
            //     Description = "Enter your JWT token"
            // });
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<TokenService>();

var app = builder.Build();

// Seed database
// using (var scope = app.Services.CreateScope())
// {
//     var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
//     await seeder.SeedAsync();
// }

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(config =>
{
    config.Endpoints.ShortNames = true;
    config.Endpoints.PrefixNameWithFirstTag = true;
    config.Errors.UseProblemDetails();
});
app.UseSwaggerGen(uiConfig: u =>
{
    u.ShowOperationIDs();
});

app.Run();

// Make the implicit Program class public for EF Core tools
public partial class Program { }