using Microsoft.AspNetCore.Mvc;
using Laptop.Data;
using Laptop.Models;
using Laptop.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Laptop.Controllers
{
    public class AdminController : Controller
    {
        private const int DefaultPageSize = 8;
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Only Admins can access this page
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            var revenueStatuses = new[] { "Paid", "Delivered" };

            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalRevenue = _context.Orders
                .Where(o => revenueStatuses.Contains(o.Status))
                .Sum(o => (decimal?)o.TotalAmount) ?? 0m;
            ViewBag.RecentOrders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToList();
            ViewBag.RecentComments = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Product)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();
            ViewBag.NewestProducts = _context.Products
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToList();
            ViewBag.OrderStatusCounts = _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Status, x => x.Count);

            var revenueStartDate = DateTime.Today.AddDays(-6);
            var recentRevenue = _context.Orders
                .Where(o => revenueStatuses.Contains(o.Status) && o.CreatedAt >= revenueStartDate)
                .Select(o => new { o.CreatedAt, o.TotalAmount })
                .AsEnumerable()
                .GroupBy(o => o.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

            ViewBag.RevenueByDay = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = revenueStartDate.AddDays(offset);
                    return new KeyValuePair<string, decimal>(
                        day.ToString("MMM dd"),
                        recentRevenue.TryGetValue(day, out var total) ? total : 0m);
                })
                .ToList();

            ViewBag.TopProducts = _context.OrderItems
                .GroupBy(i => i.Title)
                .Select(g => new { Title = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToDictionary(x => x.Title, x => x.Quantity);

            return View(); // Admin/Index.cshtml
        }

        public IActionResult Users(string? search, string? role, int page = 1)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            page = NormalizePage(page);

            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmedSearch = search.Trim();
                usersQuery = usersQuery.Where(u =>
                    u.Username.Contains(trimmedSearch) ||
                    u.Email.Contains(trimmedSearch));
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All")
            {
                usersQuery = usersQuery.Where(u => u.Role == role);
            }

            var totalCount = usersQuery.Count();
            var users = usersQuery
                .OrderBy(u => u.Username)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToList();

            var userIds = users.Select(u => u.Id).ToList();
            var usernames = users.Select(u => u.Username).ToList();

            ViewBag.UserCommentCounts = _context.Comments
                .Where(c => userIds.Contains(c.UserId))
                .GroupBy(c => c.UserId)
                .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.UserOrderCounts = _context.Orders
                .Where(o => userIds.Contains(o.UserId))
                .GroupBy(o => o.UserId)
                .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.UserCartCounts = _context.CartItems
                .Where(c => usernames.Contains(c.Username))
                .GroupBy(c => c.Username)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.Search = search ?? string.Empty;
            ViewBag.RoleFilter = string.IsNullOrWhiteSpace(role) ? "All" : role;
            SetPaginationMetadata(page, totalCount, DefaultPageSize);

            return View(users); // Admin/Users.cshtml
        }

        // Add Admin role to a user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MakeAdmin(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("Index", "Products");

            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.Role = "Admin";
                _context.SaveChanges();
            }

            return RedirectToAction("Users");
        }

        // Remove Admin role from a user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveAdmin(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("Index", "Products");

            var user = _context.Users.Find(id);
            if (user != null && user.Username != "JamesTNBS") // Prevent removing main admin
            {
                user.Role = "User";
                _context.SaveChanges();
            }

            return RedirectToAction("Users");
        }


        // Delete user + all related data (CartItems, Comments, etc.)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            // Load user with all related data
            var user = _context.Users
                .Include(u => u.CartItems)
                .Include(u => u.Comments)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Users");
            }

            // Security checks
            if (user.Username == "JamesTNBS")
            {
                TempData["Error"] = "Cannot delete the main administrator account (JamesTNBS).";
                return RedirectToAction("Users");
            }

            if (user.Role == "Admin")
            {
                TempData["Error"] = "Cannot delete another administrator account.";
                return RedirectToAction("Users");
            }

            var userCommentIds = user.Comments.Select(c => c.Id).ToList();
            var commentIdsToDelete = GetCommentThreadIds(userCommentIds);
            var commentsToDelete = _context.Comments
                .Where(c => commentIdsToDelete.Contains(c.Id))
                .ToList();

            // Manually delete related data first
            _context.CartItems.RemoveRange(user.CartItems);
            _context.Comments.RemoveRange(commentsToDelete);

            // Then delete the user
            _context.Users.Remove(user);

            _context.SaveChanges();

            TempData["Success"] = $"User '{user.Username}' and all their data (cart items and comments) have been successfully deleted.";

            return RedirectToAction("Users");
        }

        public IActionResult CartItems(string? search, int page = 1)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            page = NormalizePage(page);

            var itemsQuery = _context.CartItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmedSearch = search.Trim();
                itemsQuery = itemsQuery.Where(c =>
                    c.Username.Contains(trimmedSearch) ||
                    c.Title.Contains(trimmedSearch));
            }

            var totalCount = itemsQuery.Count();
            var items = itemsQuery
                .OrderBy(c => c.Username)
                .ThenBy(c => c.Title)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToList();

            ViewBag.Search = search ?? string.Empty;
            SetPaginationMetadata(page, totalCount, DefaultPageSize);

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCartItem(int id, int quantity)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            var item = _context.CartItems.Find(id);
            if (item != null)
            {
                item.Quantity = quantity;
                if (item.Quantity <= 0)
                    _context.CartItems.Remove(item);

                _context.SaveChanges();
            }

            return RedirectToAction("CartItems");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCartItem(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            var item = _context.CartItems.Find(id);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                _context.SaveChanges();
            }

            return RedirectToAction("CartItems");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditComment(int id, string text)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            var comment = _context.Comments.Find(id);
            if (comment == null)
                return Json(new { success = false, message = "Not found" });

            if (role != "Admin" && comment.UserId != userId)
                return Json(new { success = false, message = "Unauthorized" });

            comment.Text = text.Trim();
            _context.SaveChanges();

            return Json(new { success = true, text = comment.Text }); // ✅ MUST be JSON
        }

        public IActionResult Comments(string? search, int page = 1)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            page = NormalizePage(page);

            var rootCommentsQuery = _context.Comments
                .Where(c => c.ParentCommentId == null)
                .Include(c => c.User)
                .Include(c => c.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmedSearch = search.Trim();
                rootCommentsQuery = rootCommentsQuery.Where(c =>
                    c.Text.Contains(trimmedSearch) ||
                    c.User.Username.Contains(trimmedSearch) ||
                    c.Product.Title.Contains(trimmedSearch));
            }

            var totalCount = rootCommentsQuery.Count();
            var rootCommentIds = rootCommentsQuery
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .Select(c => c.Id)
                .ToList();

            var threadIds = GetCommentThreadIds(rootCommentIds);
            var comments = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Product)
                .Where(c => threadIds.Contains(c.Id))
                .OrderBy(c => c.CreatedAt)
                .ToList();

            ViewBag.Search = search ?? string.Empty;
            ViewBag.RootCommentIds = rootCommentIds;
            SetPaginationMetadata(page, totalCount, DefaultPageSize);

            return View(comments);
        }

        public IActionResult Orders(string? search, string? status, int page = 1)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            page = NormalizePage(page);

            var ordersQuery = _context.Orders
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmedSearch = search.Trim();
                ordersQuery = ordersQuery.Where(o =>
                    o.Username.Contains(trimmedSearch) ||
                    o.FullName.Contains(trimmedSearch) ||
                    o.Email.Contains(trimmedSearch) ||
                    o.Phone.Contains(trimmedSearch) ||
                    o.AddressLine1.Contains(trimmedSearch) ||
                    o.City.Contains(trimmedSearch));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            var totalCount = ordersQuery.Count();
            var orders = ordersQuery
                .Include(o => o.Items)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToList();

            ViewBag.Search = search ?? string.Empty;
            ViewBag.StatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status;
            SetPaginationMetadata(page, totalCount, DefaultPageSize);
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Products");

            var allowedStatuses = new[] { "Pending", "Pending Payment", "Paid", "Delivered", "Cancelled" };
            if (!allowedStatuses.Contains(status))
            {
                TempData["OrderStatusError"] = "Invalid order status.";
                return RedirectToAction(nameof(Orders));
            }

            var order = _context.Orders.Find(id);
            if (order == null)
            {
                TempData["OrderStatusError"] = "Order not found.";
                return RedirectToAction(nameof(Orders));
            }

            order.Status = status;
            _context.SaveChanges();

            TempData["OrderStatusSuccess"] = $"Order #{order.Id} updated to {status}.";
            return RedirectToAction(nameof(Orders));
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private void SetPaginationMetadata(int currentPage, int totalCount, int pageSize)
        {
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));

            ViewBag.CurrentPage = Math.Min(currentPage, totalPages);
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = currentPage > 1;
            ViewBag.HasNextPage = currentPage < totalPages;
            ViewBag.TotalCount = totalCount;
        }

        private List<int> GetCommentThreadIds(List<int> rootCommentIds)
        {
            if (!rootCommentIds.Any())
            {
                return new List<int>();
            }

            var allComments = _context.Comments
                .Select(c => new { c.Id, c.ParentCommentId })
                .ToList();

            var childLookup = allComments
                .Where(c => c.ParentCommentId.HasValue)
                .GroupBy(c => c.ParentCommentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var ids = new HashSet<int>();
            var stack = new Stack<int>(rootCommentIds);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                if (!ids.Add(currentId))
                {
                    continue;
                }

                if (!childLookup.TryGetValue(currentId, out var childIds))
                {
                    continue;
                }

                foreach (var childId in childIds)
                {
                    stack.Push(childId);
                }
            }

            return ids.ToList();
        }
    }
}
