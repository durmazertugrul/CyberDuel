using Microsoft.ML.Data;

namespace CyberDuel.Models
{
    // ML.NET'in beklediği giriş formatı
    public class MLEventData
    {
        [LoadColumn(0)] public float AttemptCount { get; set; }
        [LoadColumn(1)] public float RequestRate { get; set; }
        [LoadColumn(2)] public float PortCount { get; set; }
        [LoadColumn(3)] public float PatternFlag { get; set; }
        [LoadColumn(4)] public float RestrictedAccess { get; set; }
        [LoadColumn(5)] public bool Label { get; set; }
    }

    // ML.NET tahmin sonucunu tutan sınıf
    public class MLPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }

        [ColumnName("Probability")]
        public float Probability { get; set; }
    }
}