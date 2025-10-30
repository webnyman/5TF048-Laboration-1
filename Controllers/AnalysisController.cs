using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

/// <summary>
/// Controller for analysis features, such as providing coaching analysis for the current user.
/// </summary>
[Authorize]
public class AnalysisController : Controller
{
    private readonly IRuleCoachService _coach;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisController"/> class.
    /// </summary>
    /// <param name="coach">The rule-based coaching service.</param>
    public AnalysisController(IRuleCoachService coach) => _coach = coach;

    /// <summary>
    /// Gets the GUID of the currently logged-in user from claims.
    /// </summary>
    /// <returns>The user's GUID.</returns>
    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Displays the coaching analysis for the current user.
    /// </summary>
    /// <returns>The Coach view with the analysis model.</returns>
    public async Task<IActionResult> Coach()
    {
        var model = await _coach.AnalyzeAsync(CurrentUserId());
        return View(model); // Views/Analysis/Coach.cshtml
    }
}
