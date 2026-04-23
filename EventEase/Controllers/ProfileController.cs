using EventEase.Data;
using EventEase.Models;
using EventEase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    [Authorize]
    [Route("profile")]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppDbContext _context;

        // Max avatar file size: 200KB
        private const int MaxAvatarBytes = 200 * 1024;

        public ProfileController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /profile
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var profile = await GetOrCreateProfile(user.Id);

            var viewModel = new ManageAccountViewModel
            {
                CurrentEmail = user.Email ?? string.Empty,
                CurrentDisplayName = profile.DisplayName ?? user.UserName,
                AvatarBase64 = profile.AvatarBase64,
                Profile = new UpdateProfileViewModel
                {
                    DisplayName = profile.DisplayName ?? user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty
                }
            };

            return View(viewModel);
        }

        // POST: /profile/update
        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel Profile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["ProfileError"] = "Please correct the errors below.";
                return await ReloadView(user, activeTab: "profile");
            }

            var profile = await GetOrCreateProfile(user.Id);
            var errors = new List<string>();

            // ── Update email if changed ───────────────────────────────────────
            if (!string.Equals(user.Email, Profile.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(Profile.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    errors.Add("That email address is already in use.");
                }
                else
                {
                    user.Email = Profile.Email;
                    user.UserName = Profile.Email;
                    var result = await _userManager.UpdateAsync(user);
                    if (!result.Succeeded)
                        errors.AddRange(result.Errors.Select(e => e.Description));
                    else
                        await _signInManager.RefreshSignInAsync(user);
                }
            }

            // Update display name
            profile.DisplayName = Profile.DisplayName.Trim();
            profile.UpdatedAt = DateTime.UtcNow;
            _context.UserProfiles.Update(profile);
            await _context.SaveChangesAsync();

            if (errors.Any())
            {
                TempData["ProfileError"] = string.Join(" ", errors);
            }
            else
            {
                TempData["ProfileSuccess"] = "Profile updated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /profile/password
        [HttpPost("password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel Password)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["PasswordError"] = "Please correct the errors below.";
                return await ReloadView(user, activeTab: "password");
            }

            var result = await _userManager.ChangePasswordAsync(user, Password.CurrentPassword, Password.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                TempData["PasswordError"] = errors;
                return await ReloadView(user, activeTab: "password");
            }

            // Refresh the auth cookie so the session stays valid after password change
            await _signInManager.RefreshSignInAsync(user);
            TempData["PasswordSuccess"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /profile/avatar
        [HttpPost("avatar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["AvatarError"] = "Please select an image file.";
                return RedirectToAction(nameof(Index));
            }

            // ── Validate type ─────────────────────────────────────────────────
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(avatarFile.ContentType.ToLower()))
            {
                TempData["AvatarError"] = "Only JPEG, PNG, WebP, or GIF images are allowed.";
                return RedirectToAction(nameof(Index));
            }

            // ── Validate size ─────────────────────────────────────────────────
            if (avatarFile.Length > MaxAvatarBytes)
            {
                TempData["AvatarError"] = "Image must be smaller than 200KB.";
                return RedirectToAction(nameof(Index));
            }

            // ── Convert to Base64 data URI ────────────────────────────────────
            using var ms = new MemoryStream();
            await avatarFile.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var dataUri = $"data:{avatarFile.ContentType};base64,{base64}";

            var profile = await GetOrCreateProfile(user.Id);
            profile.AvatarBase64 = dataUri;
            profile.UpdatedAt = DateTime.UtcNow;
            _context.UserProfiles.Update(profile);
            await _context.SaveChangesAsync();

            TempData["AvatarSuccess"] = "Profile picture updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /account/manage/avatar/remove
        [HttpPost("avatar/remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var profile = await GetOrCreateProfile(user.Id);
            profile.AvatarBase64 = null;
            profile.UpdatedAt = DateTime.UtcNow;
            _context.UserProfiles.Update(profile);
            await _context.SaveChangesAsync();

            TempData["AvatarSuccess"] = "Profile picture removed.";
            return RedirectToAction(nameof(Index));
        }


        private async Task<UserProfile> GetOrCreateProfile(string userId)
        {
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId };
                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            return profile;
        }

        private async Task<IActionResult> ReloadView(IdentityUser user, string activeTab = "profile")
        {
            var profile = await GetOrCreateProfile(user.Id);
            var viewModel = new ManageAccountViewModel
            {
                CurrentEmail = user.Email ?? string.Empty,
                CurrentDisplayName = profile.DisplayName ?? user.UserName,
                AvatarBase64 = profile.AvatarBase64,
                Profile = new UpdateProfileViewModel
                {
                    DisplayName = profile.DisplayName ?? user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty
                }
            };
            ViewData["ActiveTab"] = activeTab;
            return View("Index", viewModel);
        }
    }
}