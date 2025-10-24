using PracticeLogger.DAL;
using PracticeLogger.Models;

public interface IRuleCoachService
{
    Task<PracticeCoachResult> AnalyzeAsync(Guid userId);
}

public class RuleCoachService : IRuleCoachService
{
    private readonly IPracticeSessionRepository _repo;
    public RuleCoachService(IPracticeSessionRepository repo) => _repo = repo;

    public async Task<PracticeCoachResult> AnalyzeAsync(Guid userId)
    {
        // Hämta "många" rader, sedan detaljer för säkerhets skull
        var list = await _repo.SearchAsync(userId, null, null, "date", false, 1, 5000);
        var sessions = new List<PracticeSession>();
        foreach (var li in list)
        {
            var s = await _repo.GetAsync(userId, li.SessionId);
            if (s != null) sessions.Add(s);
        }

        var res = new PracticeCoachResult();
        if (sessions.Count == 0) { res.Tips.Add("Inga pass ännu – logga några pass innan analys."); return res; }

        res.TotalSessions = sessions.Count;
        res.TotalMinutes = sessions.Sum(s => s.Minutes);
        res.AvgMinutes = Math.Round(sessions.Average(s => s.Minutes), 1);
        res.AvgIntensity = Math.Round(sessions.Average(s => (double)s.Intensity), 2);
        res.DaysActive = sessions.Select(s => s.PracticeDate.Date).Distinct().Count();

        var achievedKnown = sessions; // 'Achieved' is non-nullable bool, so all sessions have a value
        res.GoalHitRate = achievedKnown.Any() ? Math.Round(achievedKnown.Average(s => s.Achieved == true ? 1.0 : 0.0), 2) : 0;

        var withTempo = sessions.Where(s => s.TempoStart.HasValue && s.TempoEnd.HasValue).ToList();
        res.AvgTempoDelta = withTempo.Any() ? withTempo.Average(s => (double)(s.TempoEnd!.Value - s.TempoStart!.Value)) : (double?)null;

        // --- enkla regler/tips ---
        if (res.AvgIntensity < 3) res.Tips.Add("Öka intensiteten ibland (mål: 3–4) för att driva progression.");
        if (res.AvgMinutes < 20) res.Tips.Add("Dina pass är korta – prova färre, men längre kvalitetspass (≥25 min).");
        if (res.GoalHitRate < 0.6 && achievedKnown.Any()) res.Tips.Add("Måluppfyllelse <60% – gör målen tydligare och realistiska, följ upp i nästa pass.");
        if (withTempo.Count >= 10 && (res.AvgTempoDelta ?? 0) < 2) res.Tips.Add("Tempo förbättras långsamt – kör block med metronom och små steg (t.ex. +2 BPM).");

        var manyErrors = sessions.Where(s => (s.Reps ?? 0) > 0)
            .Select(s => (s.Errors ?? 0.0) / Math.Max(1, (int)(s.Reps ?? 0)));
        if (manyErrors.Any() && manyErrors.Average() > 0.2) res.Tips.Add("Fel/rep >20% – sänk tempo och jobba i korta loopar (5–7 reps).");

        if (!sessions.Any(s => s.Metronome == true)) res.Tips.Add("Träna oftare med metronom för stabil timing och tempoökning.");
        if (sessions.Select(s => s.PracticeDate.Date).Distinct().Count() < Math.Min(5, sessions.Count))
            res.Tips.Add("Öva mer regelbundet (kortare dagliga pass slår längre sporadiska).");

        return res;
    }
}
