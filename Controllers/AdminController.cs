using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PracticeLogger.Models; // ApplicationUser, ApplicationRole
using PracticeLogger.DAL;    // repo
using System.Linq;

[Authorize(Policy = "RequireAdmin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IInstrumentRepository _instRepo;
    private readonly IPracticeSessionRepository _sessRepo;

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

    // Enkel dashboard med några siffror
    public async Task<IActionResult> Dashboard()
    {
        var users = _userManager.Users.ToList();
        var roles = _roleManager.Roles.ToList();
        var instruments = await _instRepo.GetAllAsync();
        // Liten totalsiffra för pass (snabb & enkel – hämta max 1k)
        var sessions = await _sessRepo.SearchAsync(Guid.Empty, null, null, "date", true, 1, 1000);

        ViewBag.UserCount = users.Count;
        ViewBag.RoleCount = roles.Count;
        ViewBag.InstrumentCount = instruments.Count();
        ViewBag.SessionCount = sessions.Count();
        return View();
    }

    // Lista användare och vilka roller de har
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantAdmin(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        if (!await _roleManager.RoleExistsAsync("Admin"))
            await _roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });

        var res = await _userManager.AddToRoleAsync(user, "Admin");
        TempData["Flash"] = res.Succeeded ? "Admin-roll tilldelad." : string.Join(", ", res.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAdmin(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var res = await _userManager.RemoveFromRoleAsync(user, "Admin");
        TempData["Flash"] = res.Succeeded ? "Admin-roll borttagen." : string.Join(", ", res.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Users));
    }
}
