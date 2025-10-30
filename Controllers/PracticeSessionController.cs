using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PracticeLogger.DAL;
using PracticeLogger.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

/// <summary>
/// Controller for managing practice sessions, including listing, searching, creating, editing, deleting, viewing details, and generating summaries.
/// </summary>
[Authorize]
public class PracticeSessionController : Controller
{
    private readonly IPracticeSessionRepository _sessionRepo;
    private readonly IInstrumentRepository _instrumentRepo;

    /// <summary>
    /// Initializes a new instance of the <see cref="PracticeSessionController"/> class.
    /// </summary>
    /// <param name="sessionRepo">The practice session repository for data access.</param>
    /// <param name="instrumentRepo">The instrument repository for data access.</param>
    public PracticeSessionController(
        IPracticeSessionRepository sessionRepo,
        IInstrumentRepository instrumentRepo)
    {
        _sessionRepo = sessionRepo;
        _instrumentRepo = instrumentRepo;
    }

    /// <summary>
    /// Gets the GUID of the currently logged-in user from claims.
    /// </summary>
    /// <returns>The user's GUID.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user ID is missing or invalid.</exception>
    private Guid CurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var guid))
            throw new UnauthorizedAccessException("Missing or invalid user id.");
        return guid;
    }

    /// <summary>
    /// Displays a paginated, searchable, and sortable list of practice sessions for the current user.
    /// </summary>
    /// <param name="q">Optional search query.</param>
    /// <param name="instrumentId">Optional instrument ID to filter by.</param>
    /// <param name="sort">Sort field (default: "date").</param>
    /// <param name="desc">Sort descending if true.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <returns>The Index view with a list of practice sessions.</returns>
    public async Task<IActionResult> Index(
        string? q, int? instrumentId,
        string? sort = "date", bool desc = true,
        int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var userId = CurrentUserId();
        var items = await _sessionRepo.SearchAsync(userId, q, instrumentId, sort!, desc, page, pageSize);

        ViewBag.Query = q;
        ViewBag.Sort = sort;
        ViewBag.Desc = desc;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.HasPrev = page > 1;
        ViewBag.HasNext = items.Count() == pageSize;

        var instruments = await _instrumentRepo.GetAllAsync();
        ViewBag.Instruments = new SelectList(instruments, "InstrumentId", "Name", instrumentId);

        return View(items);
    }

    /// <summary>
    /// Displays the form for creating a new practice session.
    /// </summary>
    /// <returns>The Create view with a new <see cref="PracticeSession"/> model.</returns>
    public async Task<IActionResult> Create()
    {
        await PopulateInstrumentsSelectList();
        return View(new PracticeSession { PracticeDate = DateTime.Today, Intensity = 3 });
    }

    /// <summary>
    /// Handles the POST request to create a new practice session.
    /// </summary>
    /// <param name="session">The practice session to create.</param>
    /// <returns>
    /// Redirects to the Index view on success; otherwise, redisplays the form with validation errors.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PracticeSession session)
    {
        if (!ModelState.IsValid)
        {
            await PopulateInstrumentsSelectList(session.InstrumentId);
            return View(session);
        }

        session.UserId = CurrentUserId();

        var newId = await _sessionRepo.CreateAsync(session);
        TempData["Flash"] = $"New practice session saved (ID={newId}).";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the form for editing an existing practice session.
    /// </summary>
    /// <param name="id">The ID of the practice session to edit.</param>
    /// <returns>
    /// The Edit view with the session model if found; otherwise, NotFound.
    /// </returns>
    public async Task<IActionResult> Edit(int id)
    {
        var userId = CurrentUserId();
        var s = await _sessionRepo.GetAsync(userId, id);
        if (s == null) return NotFound();

        await PopulateInstrumentsSelectList(s.InstrumentId);
        return View(s);
    }

    /// <summary>
    /// Handles the POST request to update an existing practice session.
    /// </summary>
    /// <param name="session">The practice session model with updated data.</param>
    /// <returns>
    /// Redirects to the Index view on success; otherwise, redisplays the form with validation errors.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PracticeSession session)
    {
        if (!ModelState.IsValid)
        {
            await PopulateInstrumentsSelectList(session.InstrumentId);
            return View(session);
        }

        var userId = CurrentUserId();
        var ok = await _sessionRepo.UpdateAsync(userId, session);
        if (!ok) return NotFound();

        TempData["Flash"] = "Practice session updated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles the POST request to delete a practice session.
    /// </summary>
    /// <param name="id">The ID of the practice session to delete.</param>
    /// <returns>
    /// Redirects to the Index view with a flash message indicating the result.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId();
        var ok = await _sessionRepo.DeleteAsync(userId, id);
        TempData["Flash"] = ok ? "Practice session deleted." : "Delete failed.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the details of a specific practice session.
    /// </summary>
    /// <param name="id">The ID of the practice session.</param>
    /// <returns>
    /// The Details view with the session model if found; otherwise, NotFound.
    /// </returns>
    public async Task<IActionResult> Details(int id)
    {
        var userId = CurrentUserId();
        var s = await _sessionRepo.GetAsync(userId, id);
        if (s == null) return NotFound();

        var inst = await _instrumentRepo.GetAsync(s.InstrumentId);
        ViewBag.InstrumentName = inst?.Name ?? $"#{s.InstrumentId}";

        return View(s);
    }

    /// <summary>
    /// Displays a summary of practice sessions, optionally filtered by instrument and date range.
    /// </summary>
    /// <param name="instrumentId">Optional instrument ID to filter by.</param>
    /// <param name="from">Optional start date for filtering.</param>
    /// <param name="to">Optional end date for filtering.</param>
    /// <returns>The Summary view with summary data.</returns>
    public async Task<IActionResult> Summary(int? instrumentId, DateTime? from, DateTime? to)
    {
        var userId = CurrentUserId();
        var model = await _sessionRepo.GetSummaryAsync(userId, instrumentId, from, to);

        var instruments = await _instrumentRepo.GetAllAsync();
        ViewBag.Instruments = new SelectList(instruments, "InstrumentId", "Name", instrumentId);

        ViewBag.FilterText = string.Join(" · ", new[] {
            instrumentId.HasValue ? instruments.FirstOrDefault(i => i.InstrumentId == instrumentId)?.Name : null,
            from.HasValue ? $"from {from:yyyy-MM-dd}" : null,
            to.HasValue   ? $"to {to:yyyy-MM-dd}"   : null
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return View(model);
    }

    /// <summary>
    /// Populates the instruments select list for use in views.
    /// </summary>
    /// <param name="selectedId">The ID of the instrument to pre-select, if any.</param>
    private async Task PopulateInstrumentsSelectList(int? selectedId = null)
    {
        var instruments = await _instrumentRepo.GetAllAsync();
        ViewBag.Instruments = new SelectList(instruments, "InstrumentId", "Name", selectedId);
    }

    /// <summary>
    /// Handles the POST request to generate a summary for selected practice session IDs.
    /// </summary>
    /// <param name="selectedIds">The array of selected practice session IDs.</param>
    /// <returns>
    /// The Summary view with summary data for the selected sessions, or redirects to Index if none selected.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SummarySelected([FromForm] int[] selectedIds)
    {
        if (selectedIds == null || selectedIds.Length == 0)
        {
            TempData["Flash"] = "Select at least one session.";
            return RedirectToAction(nameof(Index));
        }

        var userId = CurrentUserId();
        var model = await _sessionRepo.GetSummaryByIdsAsync(userId, selectedIds);

        ViewBag.Filter = $"Selected sessions: {selectedIds.Length}";
        return View("Summary", model);
    }
}
