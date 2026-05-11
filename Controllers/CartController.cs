using Laptop.Data;
using Laptop.Extensions;
using Laptop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Laptop.Controllers
{
    public class CartController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Products");
            }

            return View(GetCartItems(username));
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var username = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Products");
            }

            var items = GetCartItems(username);
            if (!items.Any())
            {
                TempData["CheckoutError"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            var model = new CheckoutViewModel
            {
                FullName = user?.Username ?? username,
                Email = user?.Email ?? string.Empty,
                Items = items
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Add(int productId)
        {
            var username = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, requireLogin = true });
            }

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var existingItem = _context.CartItems
                .FirstOrDefault(c => c.ProductId == productId && c.Username == username);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                _context.CartItems.Update(existingItem);
            }
            else
            {
                var firstImage = product.Images?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault()?.Trim() ?? "~/images/default-laptop.jpg";

                var newItem = new CartItem
                {
                    ProductId = product.Id,
                    Title = product.Title,
                    Price = product.Price,
                    Image = firstImage,
                    Quantity = 1,
                    Username = username
                };

                _context.CartItems.Add(newItem);
            }

            _context.SaveChanges();

            var count = _context.CartItems
                .Where(c => c.Username == username)
                .Sum(c => (int?)c.Quantity) ?? 0;

            return Json(new { success = true, cartCount = count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Increase(int id)
        {
            var username = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, requireLogin = true });
            }

            var item = _context.CartItems
                .FirstOrDefault(x => x.Id == id && x.Username == username);

            if (item == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            item.Quantity++;
            _context.SaveChanges();

            return Json(CreateCartResponse(username, item));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrease(int id)
        {
            var username = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, requireLogin = true });
            }

            var item = _context.CartItems
                .FirstOrDefault(x => x.Id == id && x.Username == username);

            if (item == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            item.Quantity--;
            var removed = item.Quantity <= 0;
            if (removed)
            {
                _context.CartItems.Remove(item);
            }

            _context.SaveChanges();

            return Json(CreateCartResponse(username, item, removed));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var username = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, requireLogin = true });
            }

            var item = _context.CartItems
                .FirstOrDefault(x => x.Id == id && x.Username == username);

            if (item == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            _context.CartItems.Remove(item);
            _context.SaveChanges();

            return Json(CreateCartResponse(username, item, removed: true));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutViewModel model)
        {
            var username = HttpContext.Session.GetString("User");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.IsNullOrEmpty(username) || userId == null)
            {
                return RedirectToAction("Index", "Products");
            }

            var items = GetCartItems(username);

            if (!items.Any())
            {
                TempData["CheckoutError"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            model.Items = items;

            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            var order = new Order
            {
                UserId = userId.Value,
                Username = username,
                Status = string.Equals(model.PaymentMethod?.Trim(), "Bank Transfer", StringComparison.OrdinalIgnoreCase)
                    ? "Pending Payment"
                    : "Pending",
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                AddressLine1 = model.AddressLine1.Trim(),
                AddressLine2 = model.AddressLine2.Trim(),
                City = model.City.Trim(),
                StateOrProvince = model.StateOrProvince.Trim(),
                PostalCode = model.PostalCode.Trim(),
                PaymentMethod = model.PaymentMethod.Trim(),
                Notes = model.Notes?.Trim() ?? string.Empty,
                TotalAmount = items.Sum(x => x.Price * x.Quantity),
                CreatedAt = DateTime.Now,
                Items = items.Select(x => new OrderItem
                {
                    ProductId = x.ProductId,
                    Title = x.Title,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    Image = x.Image
                }).ToList()
            };

            _context.Orders.Add(order);

            var trackedItems = _context.CartItems.Where(x => x.Username == username).ToList();
            _context.CartItems.RemoveRange(trackedItems);
            _context.SaveChanges();

            TempData["OrderId"] = order.Id;

            return RedirectToAction(nameof(CheckoutSuccess));
        }

        [HttpGet]
        public IActionResult CheckoutSuccess()
        {
            if (TempData["OrderId"] is not int orderId)
            {
                return RedirectToAction(nameof(Index));
            }

            var username = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Products");
            }

            var order = _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == orderId && o.Username == username);

            if (order == null)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CurrentProductImages = GetCurrentProductImages(order.Items.Select(i => i.ProductId));
            return View(order);
        }

        [HttpGet]
        public IActionResult MyOrders()
        {
            var username = HttpContext.Session.GetString("User");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.IsNullOrEmpty(username) || userId == null)
            {
                return RedirectToAction("Index", "Products");
            }

            var orders = _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.UserId == userId.Value && o.Username == username)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            ViewBag.CurrentProductImages = GetCurrentProductImages(
                orders.SelectMany(o => o.Items.Select(i => i.ProductId)));

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int id)
        {
            var username = HttpContext.Session.GetString("User");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.IsNullOrEmpty(username) || userId == null)
            {
                return Json(new { success = false, requireLogin = true });
            }

            var order = _context.Orders
                .FirstOrDefault(o => o.Id == id && o.UserId == userId.Value && o.Username == username);

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            if (order.Status == "Delivered" || order.Status == "Cancelled")
            {
                return Json(new { success = false, message = $"This order is already {order.Status.ToLower()}." });
            }

            order.Status = "Cancelled";
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                status = order.Status,
                message = $"Order #{order.Id} cancelled."
            });
        }

        private object CreateCartResponse(string username, CartItem item, bool removed = false)
        {
            var cartItems = _context.CartItems
                .Where(c => c.Username == username)
                .ToList();

            var itemCount = cartItems.Sum(c => c.Quantity);
            var subtotal = cartItems.Sum(c => c.Price * c.Quantity);

            return new
            {
                success = true,
                removed,
                itemId = item.Id,
                quantity = removed ? 0 : item.Quantity,
                itemTotal = removed ? 0 : item.Price * item.Quantity,
                cartCount = itemCount,
                itemCount,
                subtotal
            };
        }

        private List<CartItem> GetCartItems(string username)
        {
            var items = _context.CartItems
                .AsNoTracking()
                .Where(c => c.Username == username)
                .OrderBy(c => c.Title)
                .ToList();

            var currentImages = GetCurrentProductImages(items.Select(i => i.ProductId));
            foreach (var item in items)
            {
                if (currentImages.TryGetValue(item.ProductId, out var currentImage) &&
                    !string.IsNullOrWhiteSpace(currentImage))
                {
                    item.Image = currentImage;
                }
            }

            return items;
        }

        private Dictionary<int, string> GetCurrentProductImages(IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            if (!ids.Any())
            {
                return new Dictionary<int, string>();
            }

            return _context.Products
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.Images })
                .AsEnumerable()
                .ToDictionary(p => p.Id, p => GetFirstProductImage(p.Images));
        }

        private static string GetFirstProductImage(string? images)
        {
            return images?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault() ?? string.Empty;
        }
    }
}
