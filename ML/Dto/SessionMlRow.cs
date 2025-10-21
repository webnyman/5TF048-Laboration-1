// ML/Dto/SessionMlRow.cs
using Microsoft.ML.Data;

public class SessionMlRow
{
    // Label (klassificering)
    [LoadColumn(0), ColumnName("Label")]
    public bool Achieved { get; set; }

    // Features
    [LoadColumn(1)] public float Minutes { get; set; }
    [LoadColumn(2)] public float Intensity { get; set; }
    [LoadColumn(3)] public float Mood { get; set; }
    [LoadColumn(4)] public float Energy { get; set; }
    [LoadColumn(5)] public float FocusScore { get; set; }
    [LoadColumn(6)] public float TempoStart { get; set; }
    [LoadColumn(7)] public float TempoEnd { get; set; }
    [LoadColumn(8)] public float Reps { get; set; }
    [LoadColumn(9)] public float Errors { get; set; }
    [LoadColumn(10)] public float PracticeType { get; set; } // 1..6

    // Härledning (kan fyllas i vid mappning)
    [LoadColumn(11)] public float DeltaTempo { get; set; }
}
