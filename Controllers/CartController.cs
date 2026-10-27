using Laptop.Data;
using Laptop.Extensions;
using Laptop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Laptop.Services;
using Microsoft.Extensions.Localization;
using Laptop.Resources;

namespace Laptop.Controllers
{
    public class CartController : BaseController
    {
        private readonly StorefrontFormatter _formatter;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CartController(ApplicationDbContext context, StorefrontFormatter formatter, IStringLocalizer<SharedResource> localizer) : base(context)
        {
            _formatter = formatter;
            _localizer = localizer;
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
                TempData["CheckoutError"] = _localizer["CartEmpty"].Value;
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

            var requestedQuantity = (existingItem?.Quantity ?? 0) + 1;
            if (requestedQuantity > product.StockQuantity)
            {
                return Json(new { success = false, message = string.Format(_localizer["InsufficientStock"].Value, product.StockQuantity), availableStock = product.StockQuantity });
            }

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
                    Currency = StorefrontFormatter.NormalizeCurrency(product.Currency),
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
                return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
            }

            var availableStock = _context.Products.Where(p => p.Id == item.ProductId).Select(p => (int?)p.StockQuantity).FirstOrDefault() ?? 0;
            if (item.Quantity + 1 > availableStock)
            {
                return Json(new { success = false, message = string.Format(_localizer["InsufficientStock"].Value, availableStock), availableStock });
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
                return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
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
                return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
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
                TempData["CheckoutError"] = _localizer["CartEmpty"].Value;
                return RedirectToAction(nameof(Index));
            }

            model.Items = items;

            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            using var transaction = _context.Database.BeginTransaction();
            foreach (var itemGroup in items.GroupBy(item => item.ProductId))
            {
                var quantity = itemGroup.Sum(item => item.Quantity);
                var updated = _context.Products
                    .Where(product => product.Id == itemGroup.Key && product.StockQuantity >= quantity)
                    .ExecuteUpdate(setters => setters.SetProperty(product => product.StockQuantity, product => product.StockQuantity - quantity));
                if (updated != 1)
                {
                    transaction.Rollback();
                    TempData["CheckoutError"] = string.Format(_localizer["StockChangedDuringCheckout"].Value, itemGroup.First().Title);
                    return RedirectToAction(nameof(Index));
                }
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
                PaymentMethod = model.PaymentMethod?.Trim() ?? "Cash on Delivery",
                Notes = model.Notes?.Trim() ?? string.Empty,
                Currency = _formatter.DisplayCurrency,
                ExchangeRate = StorefrontFormatter.VndPerUsd,
                TotalAmount = items.Sum(x => _formatter.ToDisplayAmount(x) * x.Quantity),
                CreatedAt = DateTime.Now,
                Items = items.Select(x => new OrderItem
                {
                    ProductId = x.ProductId,
                    Title = x.Title,
                    Price = _formatter.GetEffectivePrice(x),
                    Currency = StorefrontFormatter.NormalizeCurrency(x.Currency),
                    Quantity = x.Quantity,
                    Image = x.Image
                }).ToList()
            };

            _context.Orders.Add(order);

            var trackedItems = _context.CartItems.Where(x => x.Username == username).ToList();
            _context.CartItems.RemoveRange(trackedItems);
            _context.SaveChanges();
            transaction.Commit();

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
        public IActionResult ResumePayment(int id)
        {
            var username = HttpContext.Session.GetString("User");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (string.IsNullOrEmpty(username) || userId == null)
            {
                return RedirectToAction("Index", "Products");
            }

            var order = _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id && o.UserId == userId.Value && o.Username == username);

            if (order == null)
            {
                return NotFound();
            }

            if (!string.Equals(order.Status, "Pending Payment", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(order.PaymentMethod, "Bank Transfer", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(MyOrders));
            }

            ViewBag.CurrentProductImages = GetCurrentProductImages(order.Items.Select(i => i.ProductId));
            ViewBag.IsResumePayment = true;
            return View("CheckoutSuccess", order);
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

            using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
            var order = _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.Id == id && o.UserId == userId.Value && o.Username == username);

            if (order == null)
            {
                return Json(new { success = false, message = _localizer["OrderNotFound"].Value });
            }

            if (order.Status == "Delivered" || order.Status == "Cancelled")
            {
                return Json(new { success = false, message = string.Format(_localizer["OrderAlreadyStatus"].Value, _formatter.TranslateOrderStatus(order.Status)) });
            }

            foreach (var item in order.Items)
            {
                _context.Products.Where(product => product.Id == item.ProductId)
                    .ExecuteUpdate(setters => setters.SetProperty(product => product.StockQuantity, product => product.StockQuantity + item.Quantity));
            }
            order.Status = "Cancelled";
            _context.SaveChanges();
            transaction.Commit();

            return Json(new
            {
                success = true,
                status = order.Status,
                message = string.Format(_localizer["OrderCancelledNumber"].Value, order.Id)
            });
        }

        private object CreateCartResponse(string username, CartItem item, bool removed = false)
        {
            var cartItems = _context.CartItems
                .Where(c => c.Username == username)
                .ToList();
            ApplyPromotionPricing(cartItems);

            var pricedItem = cartItems.FirstOrDefault(c => c.Id == item.Id) ?? item;

            var itemCount = cartItems.Sum(c => c.Quantity);
            var subtotal = cartItems.Sum(c => _formatter.ToDisplayAmount(c) * c.Quantity);
            var availableStock = _context.Products.Where(product => product.Id == item.ProductId)
                .Select(product => (int?)product.StockQuantity).FirstOrDefault() ?? 0;

            return new
            {
                success = true,
                removed,
                itemId = item.Id,
                quantity = removed ? 0 : item.Quantity,
                availableStock,
                itemTotal = removed ? 0 : _formatter.ToDisplayAmount(pricedItem) * pricedItem.Quantity,
                itemTotalFormatted = removed ? _formatter.FormatDisplay(0) : _formatter.FormatDisplay(_formatter.ToDisplayAmount(pricedItem) * pricedItem.Quantity),
                itemPriceFormatted = removed ? string.Empty : _formatter.Format(pricedItem),
                itemBasePriceFormatted = removed ? string.Empty : _formatter.Format(pricedItem.Price, pricedItem.Currency),
                itemDiscountPercent = removed ? 0m : pricedItem.PromotionDiscountPercent ?? 0m,
                itemDiscountLabel = removed || pricedItem.PromotionDiscountPercent is not > 0m
                    ? string.Empty
                    : _localizer["PromotionPercentOff", pricedItem.PromotionDiscountPercent.Value].Value,
                cartCount = itemCount,
                itemCount,
                subtotal,
                subtotalFormatted = _formatter.FormatDisplay(subtotal)
            };
        }

        private List<CartItem> GetCartItems(string username)
        {
            var items = _context.CartItems
                .AsNoTracking()
                .Where(c => c.Username == username)
                .OrderBy(c => c.Title)
                .ToList();

            ApplyPromotionPricing(items);

            var currentImages = GetCurrentProductImages(items.Select(i => i.ProductId));
            var stockByProduct = _context.Products.AsNoTracking()
                .Where(product => items.Select(item => item.ProductId).Contains(product.Id))
                .ToDictionary(product => product.Id, product => product.StockQuantity);
            foreach (var item in items)
            {
                item.AvailableStock = stockByProduct.GetValueOrDefault(item.ProductId);
                if (currentImages.TryGetValue(item.ProductId, out var currentImage) &&
                    !string.IsNullOrWhiteSpace(currentImage))
                {
                    item.Image = currentImage;
                }
            }

            return items;
        }

        private void ApplyPromotionPricing(List<CartItem> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            var productIds = items.Select(item => item.ProductId).Distinct().ToList();
            var now = DateTimeOffset.UtcNow;
            var discounts = _context.ProductPromotions
                .AsNoTracking()
                .Where(promotion => productIds.Contains(promotion.ProductId) && promotion.IsActive &&
                    promotion.StartsAtUtc <= now && promotion.EndsAtUtc > now)
                .ToDictionary(promotion => promotion.ProductId, promotion => promotion.DiscountPercent);

            foreach (var item in items)
            {
                item.PromotionDiscountPercent = discounts.TryGetValue(item.ProductId, out var percent)
                    ? percent
                    : null;
            }
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
