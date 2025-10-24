using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class AnalysisController : Controller
{
    private readonly IRuleCoachService _coach;
    public AnalysisController(IRuleCoachService coach) => _coach = coach;

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Coach()
    {
        var model = await _coach.AnalyzeAsync(CurrentUserId());
        return View(model); // Views/Analysis/Coach.cshtml
    }
}
