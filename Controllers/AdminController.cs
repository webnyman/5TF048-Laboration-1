using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PracticeLogger.Models;
using PracticeLogger.DAL;
using System.Linq;

/// <summary>
/// Controller for administrative tasks such as user and role management, and dashboard statistics.
/// Requires the "RequireAdmin" policy for access.
/// </summary>
[Authorize(Policy = "RequireAdmin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IInstrumentRepository _instRepo;
    private readonly IPracticeSessionRepository _sessRepo;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    /// <param name="userManager">The user manager for handling user accounts.</param>
    /// <param name="roleManager">The role manager for handling roles.</param>
    /// <param name="instRepo">The instrument repository for instrument data access.</param>
    /// <param name="sessRepo">The practice session repository for session data access.</param>
    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IInstrumentRepository instRepo,
        IPracticeSessionRepository sessRepo)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _instRepo = instRepo;
        _sessRepo = sessRepo;
    }

    /// <summary>
    /// Displays a dashboard with statistics about users, roles, instruments, and practice sessions.
    /// </summary>
    /// <returns>The Dashboard view with summary data in ViewBag.</returns>
    public async Task<IActionResult> Dashboard()
    {
        var users = _userManager.Users.ToList();
        var roles = _roleManager.Roles.ToList();
        var instruments = await _instRepo.GetAllAsync();
        var sessions = await _sessRepo.SearchAsync(Guid.Empty, null, null, "date", true, 1, 1000);

        ViewBag.UserCount = users.Count;
        ViewBag.RoleCount = roles.Count;
        ViewBag.InstrumentCount = instruments.Count();
        ViewBag.SessionCount = sessions.Count();
        return View();
    }

    /// <summary>
    /// Lists all users and their assigned roles.
    /// </summary>
    /// <returns>The Users view with a list of users and their roles.</returns>
    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var model = new List<(ApplicationUser User, IList<string> Roles)>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            model.Add((u, roles));
        }
        return View(model);
    }

    /// <summary>
    /// Grants the "Admin" role to a user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>Redirects to the Users view with a flash message indicating the result.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantAdmin(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        if (!await _roleManager.RoleExistsAsync("Admin"))
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });

        var res = await _userManager.AddToRoleAsync(user, "Admin");
        TempData["Flash"] = res.Succeeded ? "Admin role granted." : string.Join(", ", res.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Revokes the "Admin" role from a user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>Redirects to the Users view with a flash message indicating the result.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAdmin(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var res = await _userManager.RemoveFromRoleAsync(user, "Admin");
        TempData["Flash"] = res.Succeeded ? "Admin role revoked." : string.Join(", ", res.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Users));
    }
}
