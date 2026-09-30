using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Laptop.Data;
using Laptop.Models;
using Laptop.Extensions;
using Laptop.Models.DTOs;
using Laptop.Services;

namespace Laptop.Controllers
{
    public class ProductsController : BaseController
    {
        private readonly IWebHostEnvironment _environment;
        private readonly StorefrontFormatter _formatter;
        private readonly Microsoft.Extensions.Localization.IStringLocalizer<Laptop.Resources.SharedResource> _localizer;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment, StorefrontFormatter formatter, Microsoft.Extensions.Localization.IStringLocalizer<Laptop.Resources.SharedResource> localizer) : base(context)
        {
            _environment = environment;
            _formatter = formatter;
            _localizer = localizer;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .AsQueryable();

            // 1. Search term
            var searchTerm = Request.Query["search"].ToString().ToLower().Trim();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    (p.Title != null && p.Title.ToLower().Contains(searchTerm)) ||
                    (p.Model != null && p.Model.ToLower().Contains(searchTerm)) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTerm)));
            }

            // 2. Price range is expressed in the visitor's selected display currency.
            var minPriceStr = Request.Query["minPrice"].ToString();
            var maxPriceStr = Request.Query["maxPrice"].ToString();

            // 3. Condition filter
            var selectedConditions = Request.Query["condition"].ToList();
            if (selectedConditions.Any())
            {
                query = query.Where(p => selectedConditions.Contains(p.Condition));
            }

            // 4. Model filter
            var selectedBrands = Request.Query["brand"]
                .Where(brand => !string.IsNullOrWhiteSpace(brand))
                .Select(brand => brand!)
                .ToList();

            if (selectedBrands.Any())
            {
                query = query.Where(p => p.Model != null &&
                    selectedBrands.Any(b => (p.Model ?? string.Empty).ToLower().Contains(b.ToLower())));
            }

            // 5. Finish currency-aware filtering and sorting in memory. Product counts are
            // small for this storefront and this avoids treating native USD and VND values alike.
            var sortBy = Request.Query["sort"].ToString().ToLower();
            var products = await query.ToListAsync();
            if (decimal.TryParse(minPriceStr, out var minPrice) && minPrice > 0)
                products = products.Where(p => _formatter.ToDisplayAmount(p) >= minPrice).ToList();
            if (decimal.TryParse(maxPriceStr, out var maxPrice) && maxPrice > 0)
                products = products.Where(p => _formatter.ToDisplayAmount(p) <= maxPrice).ToList();

            products = sortBy switch
            {
                "price-low" => products.OrderBy(p => _formatter.ToDisplayAmount(p)).ToList(),
                "price-high" => products.OrderByDescending(p => _formatter.ToDisplayAmount(p)).ToList(),
                "newest" => products.OrderByDescending(p => p.Id).ToList(),
                _ => products.OrderBy(p => p.Title).ToList()
            };

            ViewBag.Role = HttpContext.Session.GetString("Role");

            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .AsNoTracking()                   // Prevents EF from using cached data
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Role = HttpContext.Session.GetString("Role");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Products/Create
        // POST: Products/Create
        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<IFormFile>? imageFiles)
        {
            product.Seller = "Admin";
            product.Currency = StorefrontFormatter.NormalizeCurrency(product.Currency);
            product.Images = await MergeImageSourcesAsync(product.Images, imageFiles);

            if (string.IsNullOrWhiteSpace(product.Images))
            {
                ModelState.AddModelError("Images", _localizer["AddAtLeastOneImage"].Value);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Save Features
                    if (!string.IsNullOrWhiteSpace(product.Features))
                    {
                        var features = product.Features.Split('\n',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        foreach (var f in features)
                        {
                            _context.ProductFeatures.Add(new ProductFeature
                            {
                                ProductId = product.Id,
                                Feature = f
                            });
                        }
                    }

                    // Save Images
                    if (!string.IsNullOrWhiteSpace(product.Images))
                    {
                        var images = product.Images.Split(',',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        foreach (var url in images)
                        {
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = url.Trim()
                            });
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["Success"] = _localizer["ProductAddedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving product: {ex.Message}");
                }
            }

            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Model,Price,Currency,Features,Description,Images,FullDescription,Condition")] Product product, List<IFormFile>? imageFiles)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            var existingProduct = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            product.Images = await MergeImageSourcesAsync(product.Images, imageFiles);

            if (string.IsNullOrWhiteSpace(product.Images))
            {
                ModelState.AddModelError("Images", "Add at least one image URL or upload an image from your computer.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    DeleteRemovedLocalImages(existingProduct.Images, product.Images);

                    existingProduct.Title = product.Title;
                    existingProduct.Model = product.Model;
                    existingProduct.Price = product.Price;
                    existingProduct.Currency = StorefrontFormatter.NormalizeCurrency(product.Currency);
                    existingProduct.Features = product.Features;
                    existingProduct.Description = product.Description;
                    existingProduct.Images = product.Images;
                    existingProduct.FullDescription = product.FullDescription;
                    existingProduct.Condition = product.Condition;

                    _context.ProductImages.RemoveRange(existingProduct.ProductImages);

                    if (!string.IsNullOrWhiteSpace(existingProduct.Images))
                    {
                        var images = existingProduct.Images.Split(',',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        foreach (var url in images)
                        {
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = existingProduct.Id,
                                ImageUrl = url.Trim()
                            });
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Update cart items when product is edited - explicit projection
                    var cartItems = await _context.CartItems
                        .Where(c => c.ProductId == existingProduct.Id)
                        .Select(c => new CartItem
                        {
                            Id = c.Id,
                            Title = c.Title,
                            Price = c.Price,
                            Image = c.Image,
                            Quantity = c.Quantity,
                            ProductId = c.ProductId,
                            Username = c.Username
                        })
                        .ToListAsync();

                    var firstImage = existingProduct.Images?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .FirstOrDefault()?.Trim() ?? "~/images/default-laptop.jpg";

                    foreach (var item in cartItems)
                    {
                        item.Title = existingProduct.Title;
                        item.Price = existingProduct.Price;
                        item.Currency = existingProduct.Currency;
                        item.Image = firstImage;
                    }

                    _context.CartItems.UpdateRange(cartItems);
                    await _context.SaveChangesAsync();

                    TempData["ToastMessage"] = _localizer["ProductUpdatedSuccessfully"].Value;
                    TempData["ToastType"] = "success";

                    return RedirectToAction("Details", new { id = existingProduct.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(product);
        }

        // GET: Delete confirmation page (optional)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // MAIN DELETE ACTION - Used by BOTH Index and Details pages
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            try
            {
                DeleteLocalImages(product.Images);

                // Delete related records first
                _context.ProductImages.RemoveRange(_context.ProductImages.Where(x => x.ProductId == id));
                _context.ProductFeatures.RemoveRange(_context.ProductFeatures.Where(x => x.ProductId == id));
                _context.Comments.RemoveRange(_context.Comments.Where(x => x.ProductId == id));

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = _localizer["ProductDeletedSuccessfully"].Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Keep this only if you still use the old Delete form
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                DeleteLocalImages(product.Images);
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper method
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private async Task<string> MergeImageSourcesAsync(string? imageUrls, List<IFormFile>? imageFiles)
        {
            var images = (imageUrls ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (imageFiles == null || !imageFiles.Any())
            {
                return string.Join(", ", images);
            }

            var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadDirectory);

            foreach (var file in imageFiles.Where(f => f.Length > 0))
            {
                var extension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadDirectory, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                images.Add($"uploads/products/{fileName}");
            }

            return string.Join(", ", images.Distinct());
        }

        private void DeleteRemovedLocalImages(string? previousImages, string? currentImages)
        {
            var previous = (previousImages ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(IsLocalUploadPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var current = (currentImages ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(IsLocalUploadPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var removedPath in previous.Except(current))
            {
                var fullPath = Path.Combine(_environment.WebRootPath, removedPath.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }

        private void DeleteLocalImages(string? images)
        {
            var localImages = (images ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(IsLocalUploadPath)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var localImage in localImages)
            {
                var fullPath = Path.Combine(_environment.WebRootPath, localImage.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }

        private static bool IsLocalUploadPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && !path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment([FromBody] CommentDTO data)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return Json(new { success = false, requireLogin = true });
            }

            if (data == null || string.IsNullOrWhiteSpace(data.Text))
            {
                return Json(new { success = false, message = _localizer["CommentCannotBeEmpty"].Value });
            }

            if (!_context.Products.Any(p => p.Id == data.ProductId))
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            if (data.ParentCommentId.HasValue)
            {
                var parentComment = _context.Comments.FirstOrDefault(c => c.Id == data.ParentCommentId.Value);
                if (parentComment == null || parentComment.ProductId != data.ProductId)
                {
                    return Json(new { success = false, message = _localizer["ReplyTargetNotFound"].Value });
                }
            }

            var comment = new Comment
            {
                ProductId = data.ProductId,
                UserId = userId.Value,
                ParentCommentId = data.ParentCommentId,
                Text = data.Text.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            _context.SaveChanges();

            var user = _context.Users.First(u => u.Id == userId.Value);

            return Json(new
            {
                success = true,
                comment = new
                {
                    id = comment.Id,
                    productId = comment.ProductId,
                    parentCommentId = comment.ParentCommentId,
                    text = comment.Text,
                    createdAt = comment.CreatedAt.ToString("MMM dd, yyyy • hh:mm tt"),
                    username = user.Username,
                    canManage = true
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComment(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            var comment = _context.Comments.FirstOrDefault(c => c.Id == id);
            if (comment == null)
                return Json(new { success = false, message = _localizer["CommentNotFound"].Value });

            // Admin can delete any comment
            // Normal user can only delete their own comment
            if (role != "Admin" && comment.UserId != userId)
            {
                return Json(new { success = false, message = _localizer["CanOnlyDeleteOwnComments"].Value });
            }

            var parentCommentId = comment.ParentCommentId;
            var commentIdsToDelete = GetCommentThreadIds(id);
            var commentsToDelete = _context.Comments
                .Where(c => commentIdsToDelete.Contains(c.Id))
                .ToList();

            _context.Comments.RemoveRange(commentsToDelete);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                deletedIds = commentIdsToDelete,
                parentCommentId
            });
        }

        private List<int> GetCommentThreadIds(int rootCommentId)
        {
            var allComments = _context.Comments
                .Select(c => new { c.Id, c.ParentCommentId })
                .ToList();

            var childLookup = allComments
                .Where(c => c.ParentCommentId.HasValue)
                .GroupBy(c => c.ParentCommentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var ids = new List<int>();
            var stack = new Stack<int>();
            stack.Push(rootCommentId);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                ids.Add(currentId);

                if (!childLookup.TryGetValue(currentId, out var childIds))
                {
                    continue;
                }

                foreach (var childId in childIds)
                {
                    stack.Push(childId);
                }
            }

            return ids;
        }
    }
}
