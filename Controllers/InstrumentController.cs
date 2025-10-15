using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using PracticeLogger.Models;

[Authorize]
public class InstrumentController : Controller
{
    private readonly IInstrumentRepository _repo;
    public InstrumentController(IInstrumentRepository repo) => _repo = repo;

    // LISTA + SÖK
    public async Task<IActionResult> Index(string? q)
    {
        var items = await _repo.GetAllAsync(q);
        ViewBag.Query = q;
        return View(items);
    }

    // CREATE (GET)
    public IActionResult Create() => View(new Instrument());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Instrument instrument)
    {
        if (!ModelState.IsValid) return View(instrument);

        try
        {
            var id = await _repo.CreateAsync(instrument);
            TempData["Flash"] = "Instrumentet skapades.";
            return RedirectToAction("Index", "Instrument");

        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(instrument);
        }
    }


    // EDIT (GET)
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _repo.GetAsync(id);
        return item == null ? NotFound() : View(item);
    }

    // EDIT (POST)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Instrument model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var ok = await _repo.UpdateAsync(model);
            if (!ok) return NotFound();
            TempData["Flash"] = $"Instrument '{model.Name}' uppdaterades.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Kunde inte uppdatera instrument. Kontrollera att namnet är unikt.");
            return View(model);
        }
    }

    // DELETE (POST)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _repo.DeleteAsync(id);
            TempData["Flash"] = ok ? "Instrumentet raderades." : "Radering misslyckades.";
        }
        catch
        {
            // Fångar ev. FK-konflikt om instrumentet används i PracticeSessions
            TempData["Flash"] = "Instrumentet kunde inte raderas (används i övningspass).";
        }
        return RedirectToAction(nameof(Index));
    }
}
