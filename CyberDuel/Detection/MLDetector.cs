using Microsoft.ML;
using Microsoft.ML.Data;
using CyberDuel.Models;

namespace CyberDuel.Detection
{
    // ML.NET kullanarak saldırı tespiti yapan sınıf
    // Decision Tree (FastTree) algoritmasını kullandım
    public class MLDetector
    {
        private MLContext mlContext;
        private ITransformer model;
        private PredictionEngine<MLEventData, MLPrediction> engine;
        public bool Trained = false;

        public MLDetector()
        {
            // Tutarsızlığı önlemek için Seed sabitlendi
            mlContext = new MLContext(seed: 0);
        }

        public void Train(List<EventLog> data)
        {
            // EventLog listesi ML formatına çevirir
            List<MLEventData> mlData = new List<MLEventData>();

            foreach (EventLog log in data)
            {
                MLEventData item = new MLEventData();
                item.AttemptCount = log.AttemptCount;
                item.RequestRate = log.RequestRate;
                item.PortCount = log.PortCount;
                item.PatternFlag = log.PatternFlag;
                item.RestrictedAccess = log.RestrictedAccess;
                item.Label = log.IsMalicious;
                mlData.Add(item);
            }

            IDataView dataView = mlContext.Data.LoadFromEnumerable(mlData);

            // %80 eğitim, %20 test 
            var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            // Feature sütunlarını birleştirme ve FastTree ile eğitme
            var pipeline = mlContext.Transforms
                .Concatenate("Features",
                    "AttemptCount", "RequestRate", "PortCount",
                    "PatternFlag", "RestrictedAccess")
                .Append(mlContext.BinaryClassification.Trainers
                    .FastTree(labelColumnName: "Label", featureColumnName: "Features"));

            model = pipeline.Fit(split.TrainSet);
            engine = mlContext.Model.CreatePredictionEngine<MLEventData, MLPrediction>(model);
            Trained = true;

            // Test seti  performansı ölçümü ve ekrana yazdırma kısmı
            var predictions = model.Transform(split.TestSet);
            var metrics = mlContext.BinaryClassification.Evaluate(
                predictions, labelColumnName: "Label");

            Console.WriteLine("  [ML] Training complete.");
            Console.WriteLine("  [ML] Accuracy : " + metrics.Accuracy.ToString("P1"));
            Console.WriteLine("  [ML] F1 Score : " + metrics.F1Score.ToString("P1"));
        }

        public bool Predict(EventLog log, out float probability)
        {
            // Gelen logu ML formatına çevirilir tahmin yapar
            MLEventData input = new MLEventData();
            input.AttemptCount = log.AttemptCount;
            input.RequestRate = log.RequestRate;
            input.PortCount = log.PortCount;
            input.PatternFlag = log.PatternFlag;
            input.RestrictedAccess = log.RestrictedAccess;

            MLPrediction result = engine.Predict(input);
            probability = result.Probability;
            return result.PredictedLabel;
        }
    }
}