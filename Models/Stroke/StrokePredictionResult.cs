using Microsoft.ML.Data;
namespace CSProject.Models;

public class StrokePredictionResult
{
    [ColumnName("Score")]
    public float RawScore { get; set; }
    [ColumnName("PredictedLabel")]
    public bool IsAtRisk { get; set; }

    public float Probability => (float)(1 / (1 + Math.Exp(-RawScore)));

    public float StrokeRiskPercentage => Probability * 100;
}
