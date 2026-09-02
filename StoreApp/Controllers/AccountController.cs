using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Services.Abstractions;
using StoreApp.ViewModels;

namespace StoreApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogService _auditLog;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AccountController(AppDbContext db, IAuditLogService auditLog)
        {
            _db = db;
            _auditLog = auditLog;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            var verificationResult = user is null
                ? PasswordVerificationResult.Failed
                : _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

            if (user is null || !user.IsActive || verificationResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.Code)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _auditLog.LogAsync("User", user.Id, "Login", changedBy: user.Id);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (int.TryParse(userId, out var parsedUserId))
            {
                await _auditLog.LogAsync("User", parsedUserId, "Logout", changedBy: parsedUserId);
            }

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
