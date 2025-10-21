using Microsoft.ML;
using Microsoft.ML.Data;
using PracticeLogger.DAL;
using PracticeLogger.Models;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;

public class MlFeatureImportance
{
    public string Feature { get; set; } = "";
    // Klassificering (hur mycket sämre blir AUC/Accuracy om vi permuterar denna feature)
    public double? DeltaAuc { get; set; }
    public double? DeltaAccuracy { get; set; }

    // Regression (hur mycket sämre blir R^2/MAE om vi permuterar denna feature)
    public double? DeltaRSquared { get; set; }
    public double? DeltaMAE { get; set; }
}

public class MlAnalysisResult
{
    // Klassificering (Achieved)
    public BinaryClassificationMetrics AchievedMetrics { get; set; } = default!;
    public PredictionEngine<SessionMlRow, SessionAchievedPrediction> AchievedPredictor { get; set; } = default!;
    public List<MlFeatureImportance> AchievedFeatureImportance { get; set; } = new();

    // Regression (DeltaTempo)
    public RegressionMetrics DeltaTempoMetrics { get; set; } = default!;
    public PredictionEngine<SessionMlRow, DeltaTempoPrediction> DeltaTempoPredictor { get; set; } = default!;
    public List<MlFeatureImportance> DeltaFeatureImportance { get; set; } = new();
}

public interface IMlAnalysisService
{
    Task<MlAnalysisResult?> TrainAndEvaluateAsync(Guid userId, bool forceRetrain = false);
}

public class MlAnalysisService : IMlAnalysisService
{
    private readonly IPracticeSessionRepository _repo;
    private readonly IWebHostEnvironment _env;
    private static readonly MLContext _ml = new MLContext(seed: 123);

    // Featurelista (behövs för både pipeline och PFI)
    private static readonly string[] _featCols = new[] {
        nameof(SessionMlRow.Minutes),
        nameof(SessionMlRow.Intensity),
        nameof(SessionMlRow.Mood),
        nameof(SessionMlRow.Energy),
        nameof(SessionMlRow.FocusScore),
        nameof(SessionMlRow.TempoStart),
        nameof(SessionMlRow.TempoEnd),
        nameof(SessionMlRow.Reps),
        nameof(SessionMlRow.Errors),
        nameof(SessionMlRow.PracticeType),
        nameof(SessionMlRow.DeltaTempo) // används som feature i klassificering
    };

    // Stigar för modelfiler
    private const string ModelsDir = "App_Data/ml";
    private const string ClassModelName = "achieved_fastforest.zip";
    private const string RegModelName = "deltatempo_fastforest.zip";

    public MlAnalysisService(IPracticeSessionRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }

    private string ModelPath(string file) => Path.Combine(_env.ContentRootPath, ModelsDir, file);

    public async Task<MlAnalysisResult?> TrainAndEvaluateAsync(Guid userId, bool forceRetrain = false)
    {
        Directory.CreateDirectory(Path.Combine(_env.ContentRootPath, ModelsDir));

        // Bygg dataset
        var rows = await LoadUserRows(userId);
        if (rows.Count < 30) return null;

        var data = _ml.Data.LoadFromEnumerable(rows);
        var split = _ml.Data.TrainTestSplit(data, testFraction: 0.2);

        // === KLASSIFICERING (Achieved) ==================================================
        ITransformer classModel;
        DataViewSchema? classSchema;

        if (!forceRetrain && File.Exists(ModelPath(ClassModelName)))
        {
            classModel = _ml.Model.Load(ModelPath(ClassModelName), out classSchema);
        }
        else
        {
            var classPipeline =
                _ml.Transforms.Concatenate("Features", _featCols)
                .Append(_ml.BinaryClassification.Trainers.FastForest(
                    numberOfLeaves: 32, numberOfTrees: 200, minimumExampleCountPerLeaf: 5));

            classModel = classPipeline.Fit(split.TrainSet);
            _ml.Model.Save(classModel, split.TrainSet.Schema, ModelPath(ClassModelName));
        }

        // Metrics (på test)
        var classPreds = classModel.Transform(split.TestSet);
        var classMetrics = _ml.BinaryClassification.Evaluate(classPreds);
        var achievedEngine = _ml.Model.CreatePredictionEngine<SessionMlRow, SessionAchievedPrediction>(classModel);

        var classImportance = new List<MlFeatureImportance>(); // lämna tomt eller fyll senare


        // === REGRESSION (DeltaTempo) ====================================================
        // Label = DeltaTempo, ta bort DeltaTempo från featurelistan
        var regFeat = _featCols.Where(f => f != nameof(SessionMlRow.DeltaTempo)).ToArray();

        ITransformer regModel;
        DataViewSchema? regSchema;

        if (!forceRetrain && File.Exists(ModelPath(RegModelName)))
        {
            regModel = _ml.Model.Load(ModelPath(RegModelName), out regSchema);
        }
        else
        {
            var regPipeline =
                _ml.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(SessionMlRow.DeltaTempo))
                .Append(_ml.Transforms.Concatenate("Features", regFeat))
                .Append(_ml.Regression.Trainers.FastForest(
                    numberOfLeaves: 64, numberOfTrees: 300, minimumExampleCountPerLeaf: 5));

            regModel = regPipeline.Fit(split.TrainSet);
            _ml.Model.Save(regModel, split.TrainSet.Schema, ModelPath(RegModelName));
        }

        var regPreds = regModel.Transform(split.TestSet);
        var regMetrics = _ml.Regression.Evaluate(regPreds);

        // PFI för regression (på test)
        var regPfi = _ml.Regression.PermutationFeatureImportance(
             model: regModel,
             data: regPreds, // eller split.TestSet; bägge funkar
             labelColumnName: "Label",
             permutationCount: 10
         );

        var regImportance = new List<MlFeatureImportance>();
        foreach (var feat in regFeat)
        {
            if (regPfi.TryGetValue(feat, out var stats))
            {
                regImportance.Add(new MlFeatureImportance
                {
                    Feature = feat,
                    // ökad MAE = sämre → högre = viktigare
                    DeltaMAE = stats.MeanAbsoluteError.Mean,
                    // för R² tar vi negativ försämring (högre negativt = mer tapp)
                    DeltaRSquared = -stats.RSquared.Mean
                });
            }
        }
        regImportance = regImportance
            .OrderByDescending(x => x.DeltaMAE ?? 0)
            .ToList();

        var deltaEngine = _ml.Model.CreatePredictionEngine<SessionMlRow, DeltaTempoPrediction>(regModel);

        return new MlAnalysisResult
        {
            AchievedMetrics = classMetrics,
            AchievedPredictor = achievedEngine,
            AchievedFeatureImportance = classImportance, // (tom nu)

            DeltaTempoMetrics = regMetrics,
            DeltaTempoPredictor = deltaEngine,
            DeltaFeatureImportance = regImportance
        };
    }



    private async Task<List<SessionMlRow>> LoadUserRows(Guid userId)
    {
        // Hämta lista, mata sedan in detaljer (för att få alla fält)
        var items = await _repo.SearchAsync(userId, null, null, "date", false, 1, 5000);
        var rows = new List<SessionMlRow>();

        foreach (var it in items)
        {
            var s = await _repo.GetAsync(userId, it.SessionId);
            if (s == null) continue;

            float tempoStart = s.TempoStart.HasValue ? s.TempoStart.Value : 0;
            float tempoEnd = s.TempoEnd.HasValue ? s.TempoEnd.Value : 0;
            float deltaTempo = (tempoStart > 0 && tempoEnd > 0) ? (tempoEnd - tempoStart) : 0;

            rows.Add(new SessionMlRow
            {
                Achieved = s.Achieved == true,
                Minutes = s.Minutes,
                Intensity = s.Intensity,
                Mood = (s.Mood ?? 3),
                Energy = (s.Energy ?? 3),
                FocusScore = (s.FocusScore ?? 3),
                TempoStart = tempoStart,
                TempoEnd = tempoEnd,
                Reps = (s.Reps ?? 0),
                Errors = (s.Errors ?? 0),
                PracticeType = (s.PracticeType ?? 6),
                DeltaTempo = deltaTempo
            });
        }
        return rows;
    }
}
