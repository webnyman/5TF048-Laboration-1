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

        // === NÄSTA VECKA-PLAN (3–4 pass) ===
        var hasTempo = withTempo.Any();
        int baseMinutes = (int)Math.Round(Math.Max(20, res.AvgMinutes)); // minst 20 min
        int baseIntensity = (int)Math.Round(Math.Clamp(res.AvgIntensity, 2, 4)); // håll lagom fokus

        // Hitta ett “senaste” tempo som referens (eller null om saknas)
        int? lastTempo = withTempo
            .OrderByDescending(s => s.PracticeDate)
            .Select(s => s.TempoEnd ?? s.TempoStart)
            .FirstOrDefault();

        // Hjälpfunktion för veckodagar
        string[] days = { "Mån", "Ons", "Fre", "Sön" };
        int di = 0;
        string NextDay() => days[di++ % days.Length];

        // 1) Teknik + metronom (tempo progression om möjligt)
        res.NextWeekPlan.Add(new NextWeekPlanItem
        {
            Day = NextDay(),
            Focus = "Teknik – skalor/intonation",
            Minutes = baseMinutes,
            Intensity = Math.Clamp(baseIntensity + 1, 2, 5),
            Metronome = true,
            TempoTarget = hasTempo && lastTempo.HasValue ? lastTempo.Value + 2 : null,
            Notes = hasTempo
                ? "Block: 4×(6–8 rep) med +2 BPM mellan block. Sänk om felfrekvens >20%."
                : "Jobba i korta loopar (6–8 rep) och håll jämn timing. Lägg till metronom framöver."
        });

        // 2) Musikaliskt fokus + mål (öka måluppfyllelse)
        res.NextWeekPlan.Add(new NextWeekPlanItem
        {
            Day = NextDay(),
            Focus = "Musikalitet – frasering / dynamik",
            Minutes = baseMinutes,
            Intensity = baseIntensity,
            Metronome = false,
            TempoTarget = null,
            Notes = "Sätt ett mätbart mål (t.ex. fras X utan andnöd + stabil intonation). Spela in & utvärdera."
        });

        // 3) Felreduktion (om fel/rep varit hög)
        if (manyErrors.Any() && manyErrors.Average() > 0.2)
        {
            res.NextWeekPlan.Add(new NextWeekPlanItem
            {
                Day = NextDay(),
                Focus = "Felreduktion – loopa svåra takter",
                Minutes = baseMinutes - 5,
                Intensity = Math.Max(2, baseIntensity - 1),
                Metronome = true,
                TempoTarget = hasTempo && lastTempo.HasValue ? Math.Max(40, lastTempo.Value - 4) : null,
                Notes = "Bryt ner i små segment (1–2 takter), 5–7 rep/segment, långsam tempoökning när felfrekvens <10%."
            });
        }

        // 4) Uthållighet eller kvalitetspass (beroende på snitt)
        res.NextWeekPlan.Add(new NextWeekPlanItem
        {
            Day = NextDay(),
            Focus = res.AvgMinutes < 20 ? "Uthållighet – längre kvalitetspass" : "Kvalitet – precision & klang",
            Minutes = res.AvgMinutes < 20 ? baseMinutes + 10 : baseMinutes,
            Intensity = Math.Clamp(baseIntensity, 2, 4),
            Metronome = true,
            TempoTarget = hasTempo && lastTempo.HasValue ? lastTempo.Value + (res.AvgMinutes < 20 ? 3 : 1) : null,
            Notes = res.AvgMinutes < 20
                ? "Målet är längre sammanhängande fokus. Planera 2–3 delblock med kort vila."
                : "Finputsa detaljer på favoritstycke; lyssna aktivt på klang och avslut."
        });


        return res;
    }
}
