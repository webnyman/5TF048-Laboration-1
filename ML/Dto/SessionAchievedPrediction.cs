// ML/Dto/SessionAchievedPrediction.cs
using Microsoft.ML.Data;

public class SessionAchievedPrediction
{
    [ColumnName("PredictedLabel")]
    public bool Predicted { get; set; }

    public float Probability { get; set; }
    public float Score { get; set; }
}