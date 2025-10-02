using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopThoiTrangNam.Models;

namespace ShopThoiTrangNam.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly RoleManager<IdentityRole> _RoleManager;

        public UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _UserManager = userManager;
            _RoleManager = roleManager;
        }

        // Index
        public async Task<IActionResult> Index()
        {
            var users = _UserManager.Users.ToList();
            var userRoles = new List<(ApplicationUser User, IList<string> Roles)>();

            foreach (var user in users)
            {
                var roles = await _UserManager.GetRolesAsync(user);
                userRoles.Add((user, roles));
            }

            return View(userRoles);
        }

        // Create
        public IActionResult Create()
        {
            ViewBag.Roles = _RoleManager.Roles.Select(r => r.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string email, string password, string role)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await _UserManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(role) && await _RoleManager.RoleExistsAsync(role))
                    {
                        await _UserManager.AddToRoleAsync(user, role);
                    }
                    TempData["SuccessMessage"] = "User created successfully.";
                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = _RoleManager.Roles.Select(r => r.Name).ToList();
            return View();
        }

        // Delete
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _UserManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
                return RedirectToAction("Index");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(user);
        }

        // Details
        public async Task<IActionResult> Details(string id)
        {
            var user = await _UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _UserManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            return View(user);
        }

        // Edit
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _UserManager.GetRolesAsync(user);
            ViewBag.AllRoles = _RoleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.UserRoles = roles;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string email, string password, List<string> roles)
        {
            var user = await _UserManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Email = email;
            user.UserName = email;

            var result = await _UserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(user);
            }

            if (!string.IsNullOrEmpty(password))
            {
                var token = await _UserManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _UserManager.ResetPasswordAsync(user, token, password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(user);
                }
            }

            var currentRoles = await _UserManager.GetRolesAsync(user);
            roles = roles ?? new List<string>();
            var validRoles = roles.Where(r => _RoleManager.RoleExistsAsync(r).Result).ToList();

            var rolesToAdd = validRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(validRoles).ToList();

            if (rolesToAdd.Any()) await _UserManager.AddToRolesAsync(user, rolesToAdd);
            if (rolesToRemove.Any()) await _UserManager.RemoveFromRolesAsync(user, rolesToRemove);

            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction("Index");
        }
    }
}
