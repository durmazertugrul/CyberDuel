using Microsoft.ML;
using Microsoft.ML.Data;
using CyberDuel.Models;

namespace CyberDuel.Detection
{
    public class MLDetector
    {
        private MLContext mlContext;
        private ITransformer model;
        private PredictionEngine<MLEventData, MLPrediction> engine;
        public bool Trained = false;

        public MLDetector()
        {
            mlContext = new MLContext(seed: 0);
        }

        public void Train(List<EventLog> data)
        {
            List<MLEventData> mlData = new List<MLEventData>();

            foreach (EventLog log in data)
            {
                MLEventData item = new MLEventData();
                item.AttemptCount = log.AttemptCount;
                item.RequestRate = log.RequestRate;
                item.PortCount = log.PortCount;
                item.PatternFlag = log.PatternFlag;
                item.RestrictedAccess = log.RestrictedAccess;
                item.OpenPortsFound = log.OpenPortsFound;
                item.LockoutTriggered = log.LockoutTriggered;
                item.WAFBypassed = log.WAFBypassed;
                item.AttackDuration = log.AttackDuration;
                item.Label = log.IsMalicious;
                mlData.Add(item);
            }

            IDataView dataView = mlContext.Data.LoadFromEnumerable(mlData);
            var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            var pipeline = mlContext.Transforms
                .Concatenate("Features",
                    "AttemptCount", "RequestRate", "PortCount",
                    "PatternFlag", "RestrictedAccess", "OpenPortsFound",
                    "LockoutTriggered", "WAFBypassed", "AttackDuration")
                .Append(mlContext.BinaryClassification.Trainers
                    .FastTree(labelColumnName: "Label", featureColumnName: "Features"));

            model = pipeline.Fit(split.TrainSet);
            engine = mlContext.Model.CreatePredictionEngine<MLEventData, MLPrediction>(model);
            Trained = true;

            var predictions = model.Transform(split.TestSet);
            var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

            Console.WriteLine("  [ML] Training complete.");
            Console.WriteLine("  [ML] Accuracy : " + metrics.Accuracy.ToString("P1"));
            Console.WriteLine("  [ML] F1 Score : " + metrics.F1Score.ToString("P1"));
        }

        public bool Predict(EventLog log, out float probability)
        {
            MLEventData input = new MLEventData();
            input.AttemptCount = log.AttemptCount;
            input.RequestRate = log.RequestRate;
            input.PortCount = log.PortCount;
            input.PatternFlag = log.PatternFlag;
            input.RestrictedAccess = log.RestrictedAccess;
            input.OpenPortsFound = log.OpenPortsFound;
            input.LockoutTriggered = log.LockoutTriggered;
            input.WAFBypassed = log.WAFBypassed;
            input.AttackDuration = log.AttackDuration;

            MLPrediction result = engine.Predict(input);
            probability = result.Probability;
            return result.PredictedLabel;
        }
    }
}