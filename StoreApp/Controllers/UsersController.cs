using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StoreApp.Authorization;
using StoreApp.Data;
using StoreApp.Models.Entities;
using StoreApp.Services.Abstractions;
using StoreApp.ViewModels;

namespace StoreApp.Controllers
{
    // Föy 02 madde 3 / Proje_2W.pdf ekran listesi: "Kullanıcı/rol yönetimi (Dahil, sade)".
    // Kayıt olma (self-servis) burada yok; kullanıcılar sadece Yönetici/Sistem Yöneticisi
    // tarafından oluşturulur.
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public class UsersController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogService _auditLogService;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public UsersController(AppDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _db.Users
                .Include(u => u.Role)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(new CreateUserViewModel { RoleOptions = await GetRoleOptionsAsync() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kayıtlı.");
            }

            if (!ModelState.IsValid)
            {
                model.RoleOptions = await GetRoleOptionsAsync();
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                PasswordHash = string.Empty,
                RoleId = model.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditLogService.LogAsync(
                "User", user.Id, "UserCreated",
                newValue: new { user.FullName, user.Email, user.RoleId },
                changedBy: actingUserId);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return NotFound();
            }

            return View(new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleOptions = await GetRoleOptionsAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Bir yöneticinin kendi rolünü kendi kendine değiştirip sistemin tek yetkili
            // hesabını kilitlemesini önlemek için kendi kaydını bu ekrandan düzenlemesi engellenir.
            if (id == actingUserId)
            {
                ModelState.AddModelError(string.Empty, "Kendi hesabınızı bu ekrandan düzenleyemezsiniz.");
            }

            if (await _db.Users.AnyAsync(u => u.Email == model.Email && u.Id != id))
            {
                ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kayıtlı.");
            }

            if (!ModelState.IsValid)
            {
                model.RoleOptions = await GetRoleOptionsAsync();
                return View(model);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return NotFound();
            }

            var roleChanged = user.RoleId != model.RoleId;
            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.RoleId = model.RoleId;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (roleChanged)
            {
                await _auditLogService.LogAsync(
                    "User", user.Id, "UserRoleChanged", newValue: new { user.RoleId }, changedBy: actingUserId);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (id == actingUserId)
            {
                TempData["UsersError"] = "Kendi hesabınızı pasif hale getiremezsiniz.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "User", user.Id, user.IsActive ? "UserActivated" : "UserDeactivated", changedBy: actingUserId);

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetRoleOptionsAsync() =>
            await _db.Roles
                .OrderBy(r => r.Id)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();
    }
}
