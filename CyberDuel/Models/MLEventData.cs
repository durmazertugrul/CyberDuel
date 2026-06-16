using Microsoft.ML.Data;

namespace CyberDuel.Models
{
    public class MLEventData
    {
        [LoadColumn(0)] public float AttemptCount { get; set; }
        [LoadColumn(1)] public float RequestRate { get; set; }
        [LoadColumn(2)] public float PortCount { get; set; }
        [LoadColumn(3)] public float PatternFlag { get; set; }
        [LoadColumn(4)] public float RestrictedAccess { get; set; }
        [LoadColumn(5)] public float OpenPortsFound { get; set; }
        [LoadColumn(6)] public float LockoutTriggered { get; set; }
        [LoadColumn(7)] public float WAFBypassed { get; set; }
        [LoadColumn(8)] public float AttackDuration { get; set; }
        [LoadColumn(9)] public bool Label { get; set; }
    }

    public class MLPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }
        [ColumnName("Probability")]
        public float Probability { get; set; }
    }
}