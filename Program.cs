using Laptop.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Products/Index";
        options.Cookie.Name = "Laptop.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "Laptop.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseSession();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var username = context.User.FindFirstValue(ClaimTypes.Name);
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(username))
        {
            context.Session.SetString("User", username);
        }

        if (!string.IsNullOrEmpty(role))
        {
            context.Session.SetString("Role", role);
        }

        if (int.TryParse(userIdValue, out var userId))
        {
            context.Session.SetInt32("UserId", userId);
        }
    }
    else if (!string.IsNullOrEmpty(context.Session.GetString("User")))
    {
        context.Session.Clear();
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.Run();
