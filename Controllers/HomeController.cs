using Laptop.Data;
using Laptop.Extensions;
using Laptop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // required for EF Core
using System.Diagnostics;

namespace Laptop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public JsonResult SearchProducts(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var results = _context.Products
                .Where(p => p.Title.Contains(query)) // search by name
                .Select(p => new { p.Id, p.Title, p.Price }) // select fields you need
                .ToList();

            return Json(results);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}