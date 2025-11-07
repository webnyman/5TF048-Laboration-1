using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLogger.DAL;
using PracticeLogger.Models;
using PracticeLogger.Models.Api;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PracticeLogger.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "ApiJwt")] // JWT krävs som default
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

        // === Hjälpmetoder =======================================================

        private bool TryGetUserId(out Guid userId)
        {
            userId = default;
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(id, out userId);
        }

        private Guid CurrentUserId()
        {
            if (!TryGetUserId(out var userId))
                throw new InvalidOperationException("Ingen användar-id-claim hittades i token.");

            return userId;
        }

        private LinkDto Link(string rel, string href, string method)
            => new LinkDto { Rel = rel, Href = href, Method = method };

        private IEnumerable<LinkDto> BuildLinksForSession(int sessionId)
        {
            var links = new List<LinkDto>();

            var self = Url.ActionLink(nameof(GetById), values: new { id = sessionId });
            if (self != null) links.Add(Link("self", self, "GET"));

            var update = Url.ActionLink(nameof(Update), values: new { id = sessionId });
            if (update != null) links.Add(Link("update", update, "PUT"));

            var delete = Url.ActionLink(nameof(Delete), values: new { id = sessionId });
            if (delete != null) links.Add(Link("delete", delete, "DELETE"));

            return links;
        }

        private IEnumerable<LinkDto> BuildCollectionLinks(
            string? q,
            int? instrumentId,
            string sort,
            bool desc,
            int page,
            int pageSize,
            bool hasPrev,
            bool hasNext)
        {
            var links = new List<LinkDto>();

            var self = Url.ActionLink(nameof(GetList),
                values: new { q, instrumentId, sort, desc, page, pageSize });
            if (self != null) links.Add(Link("self", self, "GET"));

            var create = Url.ActionLink(nameof(Create), values: null);
            if (create != null) links.Add(Link("create", create, "POST"));

            var summary = Url.ActionLink(nameof(Summary), values: new { instrumentId });
            if (summary != null) links.Add(Link("summary", summary, "GET"));

            if (hasPrev)
            {
                var prev = Url.ActionLink(nameof(GetList),
                    values: new { q, instrumentId, sort, desc, page = page - 1, pageSize });
                if (prev != null) links.Add(Link("prev", prev, "GET"));
            }

            if (hasNext)
            {
                var next = Url.ActionLink(nameof(GetList),
                    values: new { q, instrumentId, sort, desc, page = page + 1, pageSize });
                if (next != null) links.Add(Link("next", next, "GET"));
            }

            return links;
        }

        private IEnumerable<LinkDto> BuildLinksForSummary(int? instrumentId)
        {
            var links = new List<LinkDto>();

            var self = Url.ActionLink(nameof(Summary), values: new { instrumentId });
            if (self != null) links.Add(Link("self", self, "GET"));

            var list = Url.ActionLink(nameof(GetList), values: null);
            if (list != null) links.Add(Link("list", list, "GET"));

            var selected = Url.ActionLink(nameof(SummarySelected), values: null);
            if (selected != null) links.Add(Link("summarySelected", selected, "POST"));

            return links;
        }

        // === ENDPOINTS ==========================================================

        // GET api/practicesessions?q=&instrumentId=&sort=&desc=&page=&pageSize=
        // OBS: tillåter anonymt anrop → ger "välkommen + länkar" om ej inloggad
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetList(
            [FromQuery] string? q,
            [FromQuery] int? instrumentId,
            [FromQuery] string? sort = "date",
            [FromQuery] bool desc = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // 1) Inte inloggad → "onboarding"-HATEOAS
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                var links = new List<LinkDto>();

                var self = Url.ActionLink(nameof(GetList), values: null);
                if (self != null) links.Add(Link("self", self, "GET"));

                var register = Url.ActionLink("Register", "Auth", values: null);
                if (register != null) links.Add(Link("register", register, "POST"));

                var login = Url.ActionLink("Login", "Auth", values: null);
                if (login != null) links.Add(Link("login", login, "POST"));

                return Ok(new
                {
                    message = "Välkommen till PracticeLogger API. Autentisera dig eller registrera dig för att se dina övningspass.",
                    links
                });
            }

            // 2) Inloggad → riktig lista
            var userId = CurrentUserId();

            var items = await _sessions.SearchAsync(userId, q, instrumentId, sort ?? "date", desc, page, pageSize);

            var hasPrev = page > 1;
            var hasNext = items.Count() == pageSize;

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
                Achieved = it.Achieved,
                Links = BuildLinksForSession(it.SessionId)
            });

            var result = new PagedResult<PracticeSessionListItemDto>
            {
                Items = mapped,
                Page = page,
                PageSize = pageSize,
                HasPrev = hasPrev,
                HasNext = hasNext,
                Sort = sort,
                Desc = desc,
                Query = q,
                InstrumentId = instrumentId,
                Links = BuildCollectionLinks(q, instrumentId, sort ?? "date", desc, page, pageSize, hasPrev, hasNext)
            };

            return Ok(result);
        }

        // GET api/practicesessions/123
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PracticeSessionDto>> GetById(int id)
        {
            var userId = CurrentUserId();

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
                Errors = s.Errors,
                Links = BuildLinksForSession(s.SessionId)
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
                Achieved = req.Achieved ?? false,
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

            var selfUrl = Url.ActionLink(nameof(GetById), values: new { id = newId });

            var dto = new
            {
                id = newId,
                links = selfUrl != null
                    ? BuildLinksForSession(newId)
                    : Enumerable.Empty<LinkDto>()
            };

            return CreatedAtAction(nameof(GetById), new { id = newId }, dto);
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
                Achieved = req.Achieved ?? false,
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

            // NoContent med länkar i header är också möjligt,
            // men enklast: bara 204.
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
                AvgEnergy = s.AvgEnergy,
                Links = BuildLinksForSummary(instrumentId)
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
                AvgEnergy = s.AvgEnergy,
                Links = BuildLinksForSummary(null) // eller särskilt för "selected"
            };
            return Ok(dto);
        }
    }
}
