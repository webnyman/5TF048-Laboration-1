using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PracticeLogger.DAL;
using PracticeLogger.Models;
using System;
using System.Linq;                 // <-- behövs för FirstOrDefault
using System.Security.Claims;      // <-- behövs för CurrentUserId()
using System.Threading.Tasks;

[Authorize]
public class PracticeSessionController : Controller
{
    private readonly IPracticeSessionRepository _sessionRepo;
    private readonly IInstrumentRepository _instrumentRepo;

    public PracticeSessionController(
        IPracticeSessionRepository sessionRepo,
        IInstrumentRepository instrumentRepo)
    {
        _sessionRepo = sessionRepo;
        _instrumentRepo = instrumentRepo;
    }

    // Hjälpfunktion: hämta inloggad användares GUID från claims
    private Guid CurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // LISTA + SÖK + FILTRERA + SORTERA
    // /PracticeSession/Index?q=skalor&instrumentId=1&sort=minutes&desc=true&page=1&pageSize=10
    public async Task<IActionResult> Index(
        string? q, int? instrumentId,
        string? sort = "date", bool desc = true,
        int page = 1, int pageSize = 20)
    {
        var userId = CurrentUserId();
        var items = await _sessionRepo.SearchAsync(userId, q, instrumentId, sort!, desc, page, pageSize);

        ViewBag.Query = q;
        ViewBag.Sort = sort;
        ViewBag.Desc = desc;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var instruments = await _instrumentRepo.GetAllAsync();
        ViewBag.Instruments = new SelectList(instruments, "InstrumentId", "Name", instrumentId);

        return View(items);
    }

    // CREATE (GET)
    public async Task<IActionResult> Create()
    {
        await PopulateInstrumentsSelectList();
        return View(new PracticeSession { PracticeDate = DateTime.Today, Intensity = 3 });
    }

    // CREATE (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PracticeSession session)
    {
        if (!ModelState.IsValid)
        {
            await PopulateInstrumentsSelectList(session.InstrumentId);
            return View(session);
        }

        session.UserId = CurrentUserId(); // 👈 koppla passet till inloggad användare

        var newId = await _sessionRepo.CreateAsync(session);
        TempData["Flash"] = $"Nytt övningspass sparat (ID={newId}).";
        return RedirectToAction(nameof(Index));
    }

    // EDIT (GET)
    public async Task<IActionResult> Edit(int id)
    {
        var userId = CurrentUserId();
        var s = await _sessionRepo.GetAsync(userId, id); // 👈 säkra ägarskap
        if (s == null) return NotFound();

        await PopulateInstrumentsSelectList(s.InstrumentId);
        return View(s);
    }

    // EDIT (POST)
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
        var ok = await _sessionRepo.UpdateAsync(userId, session); // 👈 säkra ägarskap i SP/SQL
        if (!ok) return NotFound();

        TempData["Flash"] = "Övningen uppdaterades.";
        return RedirectToAction(nameof(Index));
    }

    // DELETE (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId();
        var ok = await _sessionRepo.DeleteAsync(userId, id); // 👈 säkra ägarskap
        TempData["Flash"] = ok ? "Övningen raderades." : "Radering misslyckades.";
        return RedirectToAction(nameof(Index));
    }

    // DETAILS (GET)
    public async Task<IActionResult> Details(int id)
    {
        var userId = CurrentUserId();
        var s = await _sessionRepo.GetAsync(userId, id); // 👈 säkra ägarskap
        if (s == null) return NotFound();

        var inst = await _instrumentRepo.GetAsync(s.InstrumentId);
        ViewBag.InstrumentName = inst?.Name ?? $"#{s.InstrumentId}";

        return View(s);
    }

    // SUMMARY
    public async Task<IActionResult> Summary(int? instrumentId)
    {
        var userId = CurrentUserId();
        var model = await _sessionRepo.GetSummaryAsync(userId, instrumentId); // 👈 filtrera på användare

        var instruments = await _instrumentRepo.GetAllAsync();
        ViewBag.Instruments = new SelectList(instruments, "InstrumentId", "Name", instrumentId);
        ViewBag.Filter = instrumentId.HasValue
            ? instruments.FirstOrDefault(i => i.InstrumentId == instrumentId)?.Name
            : null;

        ViewData["Info"] = "Denna vy använder PracticeSummary från databasen.";
        return View(model);
    }

    // Hjälpmetod för instrumentlistan
    private async Task PopulateInstrumentsSelectList(int? selectedId = null)
    {
        var instruments = await _instrumentRepo.GetAllAsync();
        ViewBag.Instruments = new SelectList(instruments, "InstrumentId", "Name", selectedId);
    }
}
