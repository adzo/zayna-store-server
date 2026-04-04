using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zayna.Store.Server.Data;
using Zayna.Store.Server.Entities;
using Zayna.Store.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

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

builder.Services.AddAuthenticationJwtBearer(s =>
{
    s.SigningKey = builder.Configuration["JwtSettings:SigningKey"] ?? "MyLongSecretForSigningJwtTokens";
});

// Configure JWT Bearer validation parameters
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "ZaynaStore";
        options.TokenValidationParameters.ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "ZaynaStoreClient";
    });

builder.Services.AddAuthorization();
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Zayna Store";
            s.Description = "Zayna Store API for our new Store";
            s.Version = "v1";
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
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();

// Make the implicit Program class public for EF Core tools
public partial class Program { }