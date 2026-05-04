using Microsoft.AspNetCore.Mvc;
using Laptop.Data;
using System.Linq;

namespace Laptop.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public JsonResult SearchProducts(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            query = query.Trim().ToLower();

            var results = _context.Products
                .Where(p =>
                    (p.Title != null && p.Title.ToLower().Contains(query)) ||
                    (p.Model != null && p.Model.ToLower().Contains(query)) ||
                    (p.Description != null && p.Description.ToLower().Contains(query)))
                .Select(p => new
                {
                    p.Id,                    // Must have Id
                    Title = p.Title ?? "Unknown",
                    Model = p.Model ?? "",
                    Price = p.Price
                })
                .Take(10)
                .ToList();

            return Json(results);
        }
    }
}