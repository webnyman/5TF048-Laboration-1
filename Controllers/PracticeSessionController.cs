using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PracticeLogger.DAL;
using PracticeLogger.Models;
using System;
using System.Threading.Tasks;

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

    // LISTA + SÖK + FILTRERA + SORTERA
    // /PracticeSession/Index?q=skalor&instrumentId=1&sort=minutes&desc=true&page=1&pageSize=10
    public async Task<IActionResult> Index(
        string? q, int? instrumentId,
        string? sort = "date", bool desc = true,
        int page = 1, int pageSize = 20)
    {
        var (items, total) = await _sessionRepo.SearchAsync(q, instrumentId, sort, desc, page, pageSize);

        ViewBag.Query = q;
        ViewBag.Sort = sort;
        ViewBag.Desc = desc;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;

        // Dropdown för instrument
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

        var newId = await _sessionRepo.CreateAsync(session);
        TempData["Flash"] = $"Ny övning sparad (ID={newId}).";
        return RedirectToAction(nameof(Index));
    }

    // EDIT (GET)
    public async Task<IActionResult> Edit(int id)
    {
        var s = await _sessionRepo.GetAsync(id);
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

        var ok = await _sessionRepo.UpdateAsync(session);
        if (!ok) return NotFound();

        TempData["Flash"] = "Övningen uppdaterades.";
        return RedirectToAction(nameof(Index));
    }

    // DELETE (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _sessionRepo.DeleteAsync(id);
        TempData["Flash"] = ok ? "Övningen raderades." : "Radering misslyckades.";
        return RedirectToAction(nameof(Index));
    }

    // (valfritt) DETAILS (GET)
    public async Task<IActionResult> Details(int id)
    {
        var s = await _sessionRepo.GetAsync(id);
        if (s == null) return NotFound();
        // visa ett enkelt read-only-kort, eller återanvänd Edit-vy i readonly-läge
        return View(s);
    }

    // Hjälpmetod för instrumentlistan
    private async Task PopulateInstrumentsSelectList(int? selectedId = null)
    {
        var instruments = await _instrumentRepo.GetAllAsync();
        ViewBag.Instruments = new SelectList(instruments, "InstrumentId", "Name", selectedId);
    }
}
