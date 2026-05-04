using Laptop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

public class BaseController : Controller
{
    protected readonly ApplicationDbContext _context;

    public BaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var username = HttpContext.Session.GetString("User");

        if (!string.IsNullOrEmpty(username))
        {
            var count = _context.CartItems
                .Where(c => c.Username == username)
                .Sum(c => (int?)c.Quantity) ?? 0;

            ViewBag.CartCount = count;
        }
        else
        {
            ViewBag.CartCount = 0;
        }

        base.OnActionExecuting(context);
    }
}