using Laptop.Data;
using Laptop.Extensions;
using Laptop.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Laptop.Security;
using Microsoft.Extensions.Localization;
using Laptop.Resources;

namespace Laptop.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AccountController(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer) : base(context)
        {
            _localizer = localizer;
        }


        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = _localizer["AllFieldsRequired"].Value });
            }

            if (_context.Users.Any(u => u.Username.ToLower() == username.ToLower()))
            {
                return Json(new { success = false, message = _localizer["UsernameAlreadyExists"].Value });
            }

            if (_context.Users.Any(u => u.Email.ToLower() == email.ToLower()))
            {
                return Json(new { success = false, message = _localizer["EmailAlreadyExists"].Value });
            }

            var user = new User
            {
                Username = username,
                Email = email,
                Role = "User"
            };

            user.Password = PasswordSecurity.HashPassword(user, password);

            _context.Users.Add(user);
            _context.SaveChanges();

            return Json(new { success = true, message = _localizer["RegistrationSuccessful"].Value });
        }

        // LOGIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null || !PasswordSecurity.VerifyPassword(user, password, out var needsUpgrade))
            {
                return Json(new { success = false, message = _localizer["InvalidUsernameOrPassword"].Value });
            }

            if (needsUpgrade)
            {
                user.Password = PasswordSecurity.HashPassword(user, password);
                _context.SaveChanges();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });

            HttpContext.Session.SetString("User", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetInt32("UserId", user.Id);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string usernameOrEmail, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Json(new { success = false, message = _localizer["UsernameOrEmailAndNewPasswordRequired"].Value });
            }

            if (newPassword.Length < 6)
            {
                return Json(new { success = false, message = _localizer["PasswordMinLength"].Value });
            }

            var lookup = usernameOrEmail.Trim().ToLower();
            var user = _context.Users.FirstOrDefault(u =>
                u.Username.ToLower() == lookup ||
                u.Email.ToLower() == lookup);

            if (user == null)
            {
                return Json(new { success = false, message = _localizer["NoAccountFound"].Value });
            }

            user.Password = PasswordSecurity.HashPassword(user, newPassword);
            _context.SaveChanges();

            return Json(new { success = true, message = _localizer["PasswordResetSuccessLogin"].Value });
        }

        // LOGOUT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/Cart"))
            {
                return RedirectToAction("Index", "Products");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Products");
        }
    }
}
