using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLogger.DAL;
using PracticeLogger.Models;

namespace PracticeLogger.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // kräver inloggning
    public class PracticeSessionsController : ControllerBase
    {
        private readonly IPracticeSessionRepository _sessions;
        private readonly IInstrumentRepository _instruments;

        public PracticeSessionsController(
            IPracticeSessionRepository sessions,
            IInstrumentRepository instruments)
        {
            _sessions = sessions;
            _instruments = instruments;
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = default;
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out userId);
        }

        private Guid CurrentUserId()
            => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET api/practicesessions?q=&instrumentId=&sort=&desc=&page=&pageSize=
        [HttpGet]
        public async Task<ActionResult<PagedResult<PracticeSessionListItemDto>>> GetList(
            [FromQuery] string? q,
            [FromQuery] int? instrumentId,
            [FromQuery] string? sort = "date",
            [FromQuery] bool desc = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = CurrentUserId();

            var items = await _sessions.SearchAsync(userId, q, instrumentId, sort ?? "date", desc, page, pageSize);

            // OBS: SearchAsync returnerar bara "en sida". HasNext/HasPrev sätts enligt din logik:
            var hasPrev = page > 1;
            var hasNext = items.Count() == pageSize; // enkel tumregel

            var mapped = items.Select(it => new PracticeSessionListItemDto
            {
                SessionId = it.SessionId,
                PracticeDate = it.PracticeDate,
                Minutes = it.Minutes,
                Intensity = it.Intensity,
                Focus = it.Focus,
                InstrumentId = it.InstrumentId,
                InstrumentName = it.InstrumentName,
                PracticeType = it.PracticeType,
                Goal = it.Goal,
                Achieved = it.Achieved
            });

            return Ok(new PagedResult<PracticeSessionListItemDto>
            {
                Items = mapped,
                Page = page,
                PageSize = pageSize,
                HasPrev = hasPrev,
                HasNext = hasNext,
                Sort = sort,
                Desc = desc,
                Query = q,
                InstrumentId = instrumentId
            });
        }

        // GET api/practicesessions/123
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PracticeSessionDto>> GetById(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { error = "Not logged in. Supply auth cookie (or JWT) when calling the API." });

            var s = await _sessions.GetAsync(userId, id);
            if (s == null) return NotFound();

            var inst = await _instruments.GetAsync(s.InstrumentId);

            var dto = new PracticeSessionDto
            {
                SessionId = s.SessionId,
                UserId = s.UserId,
                InstrumentId = s.InstrumentId,
                InstrumentName = inst?.Name ?? "",
                PracticeDate = s.PracticeDate,
                Minutes = s.Minutes,
                Intensity = s.Intensity,
                Focus = s.Focus,
                Comment = s.Comment,
                PracticeType = s.PracticeType,
                Goal = s.Goal,
                Achieved = s.Achieved,
                Mood = s.Mood,
                Energy = s.Energy,
                FocusScore = s.FocusScore,
                TempoStart = s.TempoStart,
                TempoEnd = s.TempoEnd,
                Metronome = s.Metronome,
                Reps = s.Reps,
                Errors = s.Errors
            };

            return Ok(dto);
        }

        // POST api/practicesessions
        [HttpPost]
        public async Task<ActionResult<PracticeSessionDto>> Create([FromBody] CreatePracticeSessionRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();

            var s = new PracticeSession
            {
                UserId = userId,
                InstrumentId = req.InstrumentId,
                PracticeDate = req.PracticeDate,
                Minutes = req.Minutes,
                Intensity = req.Intensity,
                Focus = req.Focus,
                Comment = req.Comment,
                PracticeType = req.PracticeType,
                Goal = req.Goal,
                Achieved = req.Achieved ?? false, // Fix: hantera null-värde
                Mood = req.Mood,
                Energy = req.Energy,
                FocusScore = req.FocusScore,
                TempoStart = req.TempoStart,
                TempoEnd = req.TempoEnd,
                Metronome = req.Metronome ?? false,
                Reps = req.Reps,
                Errors = req.Errors
            };

            var newId = await _sessions.CreateAsync(s);
            return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
        }

        // PUT api/practicesessions/123
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePracticeSessionRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = CurrentUserId();

            var s = new PracticeSession
            {
                SessionId = id,
                UserId = userId,
                InstrumentId = req.InstrumentId,
                PracticeDate = req.PracticeDate,
                Minutes = req.Minutes,
                Intensity = req.Intensity,
                Focus = req.Focus,
                Comment = req.Comment,
                PracticeType = req.PracticeType,
                Goal = req.Goal,
                Achieved = req.Achieved ?? false, // Fix: hantera null-värde
                Mood = req.Mood,
                Energy = req.Energy,
                FocusScore = req.FocusScore,
                TempoStart = req.TempoStart,
                TempoEnd = req.TempoEnd,
                Metronome = req.Metronome ?? false,
                Reps = req.Reps,
                Errors = req.Errors
            };

            var ok = await _sessions.UpdateAsync(userId, s);
            if (!ok) return NotFound();
            return NoContent();
        }

        // DELETE api/practicesessions/123
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = CurrentUserId();
            var ok = await _sessions.DeleteAsync(userId, id);
            return ok ? NoContent() : NotFound();
        }

        // GET api/practicesessions/summary?instrumentId=1
        [HttpGet("summary")]
        public async Task<ActionResult<PracticeSummaryDto>> Summary([FromQuery] int? instrumentId)
        {
            var userId = CurrentUserId();
            var s = await _sessions.GetSummaryAsync(userId, instrumentId);

            var dto = new PracticeSummaryDto
            {
                TotalMinutes = s.TotalMinutes,
                AvgPerDay = s.AvgPerDay,
                DistinctActiveDays = s.DistinctActiveDays,
                EntriesCount = s.EntriesCount,
                MinutesPerInstrument = s.MinutesPerInstrument,
                MinutesPerIntensity = s.MinutesPerIntensity,
                MinutesPerPracticeType = s.MinutesPerPracticeType,
                PassWithTempo = s.PassWithTempo,
                AvgTempoDelta = s.AvgTempoDelta,
                AvgMood = s.AvgMood,
                AvgEnergy = s.AvgEnergy
            };
            return Ok(dto);
        }

        // POST api/practicesessions/summary/selected
        [HttpPost("summary/selected")]
        public async Task<ActionResult<PracticeSummaryDto>> SummarySelected([FromBody] int[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
                return BadRequest("Välj minst ett pass.");

            var userId = CurrentUserId();
            var s = await _sessions.GetSummaryByIdsAsync(userId, selectedIds);

            var dto = new PracticeSummaryDto
            {
                TotalMinutes = s.TotalMinutes,
                AvgPerDay = s.AvgPerDay,
                DistinctActiveDays = s.DistinctActiveDays,
                EntriesCount = s.EntriesCount,
                MinutesPerInstrument = s.MinutesPerInstrument,
                MinutesPerIntensity = s.MinutesPerIntensity,
                MinutesPerPracticeType = s.MinutesPerPracticeType,
                PassWithTempo = s.PassWithTempo,
                AvgTempoDelta = s.AvgTempoDelta,
                AvgMood = s.AvgMood,
                AvgEnergy = s.AvgEnergy
            };
            return Ok(dto);
        }
    }
}
