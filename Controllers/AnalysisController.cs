using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class AnalysisController : Controller
{
    private readonly IMlAnalysisService _ml;
    public AnalysisController(IMlAnalysisService ml) => _ml = ml;

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(bool retrain = false)
    {
        var res = await _ml.TrainAndEvaluateAsync(CurrentUserId(), forceRetrain: retrain);
        if (res == null)
        {
            ViewBag.Message = "För lite data för att träna en modell.";
            return View();
        }

        // Example what-if
        var sample = new SessionMlRow
        {
            Minutes = 45,
            Intensity = 4,
            Mood = 4,
            Energy = 4,
            FocusScore = 4,
            TempoStart = 90,
            TempoEnd = 110,
            Reps = 20,
            Errors = 2,
            PracticeType = 2,
            DeltaTempo = 20
        };
        var pred = res.AchievedPredictor.Predict(sample);
        var delta = res.DeltaTempoPredictor.Predict(sample);

        ViewBag.AchievedAccuracy = res.AchievedMetrics.Accuracy;
        ViewBag.AchievedAuc = res.AchievedMetrics.AreaUnderRocCurve;

        ViewBag.DeltaRSquared = res.DeltaTempoMetrics.RSquared;
        ViewBag.DeltaMae = res.DeltaTempoMetrics.MeanAbsoluteError;

        ViewBag.SampleProb = pred.Probability;
        ViewBag.SampleDelta = delta.Score;

        ViewBag.AchievedFI = res.AchievedFeatureImportance;
        ViewBag.DeltaFI = res.DeltaFeatureImportance;

        return View();
    }
}
