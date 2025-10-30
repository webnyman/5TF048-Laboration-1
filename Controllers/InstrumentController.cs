using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PracticeLogger.Models;

/// <summary>
/// Controller for managing musical instruments, including listing, searching, creating, editing, and deleting instruments.
/// </summary>
[Authorize]
public class InstrumentController : Controller
{
    private readonly IInstrumentRepository _repo;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentController"/> class.
    /// </summary>
    /// <param name="repo">The instrument repository for data access.</param>
    public InstrumentController(IInstrumentRepository repo) => _repo = repo;

    /// <summary>
    /// Displays a list of instruments, optionally filtered by a search query.
    /// </summary>
    /// <param name="q">An optional search query to filter instruments.</param>
    /// <returns>The Index view with a list of instruments.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? q)
    {
        var items = await _repo.GetAllAsync(q);
        ViewBag.Query = q;
        return View(items);
    }

    /// <summary>
    /// Displays the form for creating a new instrument.
    /// Only accessible to users with the "RequireAdmin" policy.
    /// </summary>
    /// <returns>The Create view with a new <see cref="Instrument"/> model.</returns>
    [Authorize(Policy = "RequireAdmin")]
    public IActionResult Create() => View(new Instrument());

    /// <summary>
    /// Handles the POST request to create a new instrument.
    /// Only accessible to users with the "RequireAdmin" policy.
    /// </summary>
    /// <param name="instrument">The instrument to create.</param>
    /// <returns>
    /// Redirects to the Index view on success; otherwise, redisplays the form with validation errors.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create(Instrument instrument)
    {
        if (!ModelState.IsValid) return View(instrument);

        try
        {
            var id = await _repo.CreateAsync(instrument);
            TempData["Flash"] = "Instrument created.";
            return RedirectToAction("Index", "Instrument");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(instrument);
        }
    }

    /// <summary>
    /// Displays the form for editing an existing instrument.
    /// Only accessible to users with the "RequireAdmin" policy.
    /// </summary>
    /// <param name="id">The ID of the instrument to edit.</param>
    /// <returns>
    /// The Edit view with the instrument model if found; otherwise, NotFound.
    /// </returns>
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _repo.GetAsync(id);
        return item == null ? NotFound() : View(item);
    }

    /// <summary>
    /// Handles the POST request to update an existing instrument.
    /// Only accessible to users with the "RequireAdmin" policy.
    /// </summary>
    /// <param name="model">The instrument model with updated data.</param>
    /// <returns>
    /// Redirects to the Index view on success; otherwise, redisplays the form with validation errors.
    /// </returns>
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Edit(Instrument model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var ok = await _repo.UpdateAsync(model);
            if (!ok) return NotFound();
            TempData["Flash"] = $"Instrument '{model.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Could not update instrument. Ensure the name is unique.");
            return View(model);
        }
    }

    /// <summary>
    /// Handles the POST request to delete an instrument.
    /// Only accessible to users with the "RequireAdmin" policy.
    /// </summary>
    /// <param name="id">The ID of the instrument to delete.</param>
    /// <returns>
    /// Redirects to the Index view with a flash message indicating the result.
    /// </returns>
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _repo.DeleteAsync(id);
            TempData["Flash"] = ok ? "Instrument deleted." : "Delete failed.";
        }
        catch
        {
            TempData["Flash"] = "Instrument could not be deleted (it is used in practice sessions).";
        }
        return RedirectToAction(nameof(Index));
    }
}
