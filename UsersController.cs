using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRooms.Controllers
{
    // The Admin's Users page (Task 7): every account by email, a Staff label
    // on accounts that already have the role, and a Make staff button for
    // everyone else. Only the Admin may open it or use the button.
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _user_manager;

        public UsersController(UserManager<IdentityUser> user_manager)
        {
            _user_manager = user_manager;
        }

        // GET /users: every account by email
        [HttpGet("users")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Users";
            List<IdentityUser> users = await _user_manager.Users
                .OrderBy(user => user.Email)
                .ToListAsync();

            // Which accounts are Admin or Staff, by Id, for the view
            Dictionary<string, bool> is_admin = new();
            Dictionary<string, bool> is_staff = new();
            foreach (IdentityUser each_user in users)
            {
                is_admin[each_user.Id] = await _user_manager.IsInRoleAsync(each_user, "Admin");
                is_staff[each_user.Id] = await _user_manager.IsInRoleAsync(each_user, "Staff");
            }
            ViewBag.IsAdmin = is_admin;
            ViewBag.IsStaff = is_staff;

            return View(users);
        }

        // POST /users/{id}/make-staff: give that account the Staff role and
        // come back to the list with a message. A POST, because it changes data.
        [HttpPost("users/{id}/make-staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeStaff(string id)
        {
            IdentityUser? user = await _user_manager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Message"] = "That account is gone";
                return RedirectToAction("Index");
            }
            if (!await _user_manager.IsInRoleAsync(user, "Staff"))
            {
                await _user_manager.AddToRoleAsync(user, "Staff");
            }
            TempData["Message"] = $"{user.Email} is now Staff";
            return RedirectToAction("Index");
        }
    }
}
