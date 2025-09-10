using System.Text.Json;
using PracticeLogger.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq;

namespace PracticeLogger.Controllers
{
    /// <summary>
    /// Controller for managing practice log entries, including listing, creation, and summary calculations.
    /// </summary>
    public class PracticeController : Controller
    {
        private const string SessionKey = "practice_entries";

        /// <summary>
        /// Displays all practice entries from the session and shows the current week number.
        /// Also displays a flash message and the last used instrument if available.
        /// </summary>
        /// <returns>The Start view with a list of <see cref="PracticeEntry"/>.</returns>
        public IActionResult Start()
        {
            var entries = GetEntries();

            ViewBag.Week = System.Globalization.ISOWeek.GetWeekOfYear(DateTime.Today);
            ViewData["Flash"] = TempData["Flash"];

            // Show last used instrument from cookie if available
            if (Request.Cookies.TryGetValue("lastInstrument", out var lastInstr))
                ViewBag.LastInstrument = lastInstr;

            return View(entries);
        }

        /// <summary>
        /// Displays the form for creating a new practice entry, pre-filling the instrument if available in cookies.
        /// </summary>
        /// <returns>The Create view with a new <see cref="PracticeEntry"/> model.</returns>
        public IActionResult Create()
        {
            var model = new PracticeEntry { Date = DateTime.Today };

            // Pre-fill instrument from cookie if available
            if (Request.Cookies.TryGetValue("lastInstrument", out var lastInstr))
                model.Instrument = lastInstr;

            return View(model);
        }

        /// <summary>
        /// Handles the POST request to create a new practice entry.
        /// Saves the entry to session and updates the last used instrument in cookies.
        /// </summary>
        /// <param name="entry">The practice entry submitted from the form.</param>
        /// <returns>
        /// Redirects to <see cref="Start"/> on success; otherwise, redisplays the form with validation errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PracticeEntry entry)
        {
            if (!ModelState.IsValid)
                return View(entry);

            var entries = GetEntries();
            entries.Add(entry);
            SaveEntries(entries);

            // Save last used instrument in cookie (7 days)
            Response.Cookies.Append("lastInstrument", entry.Instrument,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });

            TempData["Flash"] = $"La till {entry.Minutes} min {entry.Instrument} – {entry.Focus} ({entry.Date:yyyy-MM-dd}).";
            return RedirectToAction(nameof(Start));
        }

        /// <summary>
        /// Displays a summary of practice entries, with optional filtering by instrument.
        /// Calculates total minutes, average per day, minutes per instrument, minutes per intensity,
        /// number of distinct active days, and total entry count.
        /// </summary>
        /// <param name="instrument">Optional instrument name to filter the summary.</param>
        /// <returns>The Summary view with a <see cref="PracticeSummary"/> model.</returns>
        public IActionResult Summary(string? instrument)
        {
            var entries = GetEntries();

            // Optionally filter by instrument
            if (!string.IsNullOrWhiteSpace(instrument))
            {
                entries = entries
                    .Where(e => e.Instrument.Equals(instrument, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                ViewBag.Filter = instrument;
            }

            // Calculate totals and averages
            var total = entries.Sum(e => e.Minutes);
            var distinctDays = entries.Select(e => e.Date.Date).Distinct().Count();
            var avgPerDay = distinctDays == 0 ? 0 : Math.Round((double)total / distinctDays, 1);

            // Calculate minutes per instrument
            var minutesPerInstrument = entries
                .GroupBy(e => e.Instrument ?? "(Unknown)")
                .OrderByDescending(g => g.Sum(x => x.Minutes))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Minutes));

            // Calculate minutes per intensity (1–5)
            var minutesPerIntensity = entries
                .GroupBy(e => e.Intensity)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Minutes));

            // Ensure all intensity levels 1..5 are present
            for (int i = 1; i <= 5; i++)
            {
                if (!minutesPerIntensity.ContainsKey(i))
                    minutesPerIntensity[i] = 0;
            }

            var model = new PracticeSummary
            {
                TotalMinutes = total,
                AvgPerDay = avgPerDay,
                MinutesPerInstrument = minutesPerInstrument,
                MinutesPerIntensity = minutesPerIntensity
                    .OrderBy(kv => kv.Key)
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                DistinctActiveDays = distinctDays,
                EntriesCount = entries.Count
            };

            ViewData["Info"] = "This view uses a strongly typed Model (PracticeSummary).";
            return View(model);
        }

        /// <summary>
        /// Displays the error page for the Practice section.
        /// Returns the Error view from <c>Views/Practice/Error.cshtml</c> if it exists,
        /// otherwise falls back to <c>Views/Shared/Error.cshtml</c>.
        /// </summary>
        /// <returns>The Error view.</returns>
        public IActionResult Error() => View();


        /// <summary>
        /// Retrieves the list of practice entries from the session.
        /// </summary>
        /// <returns>A list of <see cref="PracticeEntry"/>.</returns>
        private List<PracticeEntry> GetEntries()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json)) return new List<PracticeEntry>();
            return JsonSerializer.Deserialize<List<PracticeEntry>>(json) ?? new List<PracticeEntry>();
        }

        /// <summary>
        /// Saves the list of practice entries to the session.
        /// </summary>
        /// <param name="entries">The list of <see cref="PracticeEntry"/> to save.</param>
        private void SaveEntries(List<PracticeEntry> entries)
        {
            var json = JsonSerializer.Serialize(entries);
            HttpContext.Session.SetString(SessionKey, json);
        }
    }
}