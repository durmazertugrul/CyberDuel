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

        private static readonly string[] FeatureNames = {
            "AttemptCount","RequestRate","PortCount","PatternFlag","RestrictedAccess",
            "OpenPortsFound","LockoutTriggered","WAFBypassed","AttackDuration"
        };
        private static readonly float[] NormalBaseline = { 2f, 50f, 3f, 0f, 0f, 0.5f, 0f, 0f, 1f };

        public MLDetector() { mlContext = new MLContext(seed: 0); }

        public void Train(List<EventLog> data)
        {
            List<MLEventData> mlData = new List<MLEventData>();
            foreach (EventLog log in data)
                mlData.Add(new MLEventData
                {
                    AttemptCount = log.AttemptCount,
                    RequestRate = log.RequestRate,
                    PortCount = log.PortCount,
                    PatternFlag = log.PatternFlag,
                    RestrictedAccess = log.RestrictedAccess,
                    OpenPortsFound = log.OpenPortsFound,
                    LockoutTriggered = log.LockoutTriggered,
                    WAFBypassed = log.WAFBypassed,
                    AttackDuration = log.AttackDuration,
                    Label = log.IsMalicious
                });

            IDataView dataView = mlContext.Data.LoadFromEnumerable(mlData);
            var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            var pipeline = mlContext.Transforms
                .Concatenate("Features", "AttemptCount", "RequestRate", "PortCount", "PatternFlag",
                    "RestrictedAccess", "OpenPortsFound", "LockoutTriggered", "WAFBypassed", "AttackDuration")
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
            var result = engine.Predict(BuildInput(log));
            probability = result.Probability;
            return result.PredictedLabel;
        }

        // Gerçek ML interpretability: single-sample perturbation
        public string ExplainDecision(EventLog log, float originalProbability)
        {
            if (!Trained || originalProbability < 0.30f) return "No significant anomaly signal.";
            float[] values = GetFeatureValues(log);
            var contributions = new List<(string name, float impact)>();
            for (int i = 0; i < FeatureNames.Length; i++)
            {
                if (values[i] <= NormalBaseline[i]) continue;
                EventLog modified = CloneLog(log);
                SetFeature(modified, i, NormalBaseline[i]);
                float modifiedProb; Predict(modified, out modifiedProb);
                float impact = originalProbability - modifiedProb;
                if (impact > 0.02f) contributions.Add((FeatureNames[i] + "=" + values[i].ToString("F0"), impact));
            }
            if (contributions.Count == 0) return "ML flagged via multi-feature combination.";
            contributions = contributions.OrderByDescending(c => c.impact).Take(3).ToList();
            return "ML contributors: " + string.Join(" | ", contributions.Select(c => c.name + " [+" + c.impact.ToString("F2") + "]"));
        }

        public string ExplainMiss(EventLog log)
        {
            if (log.AttackType == AttackType.PortScan && log.PortCount <= 15 && log.OpenPortsFound < 3)
                return "PortScan: PortCount=" + (int)log.PortCount + " (≤15), OpenPorts=" + (int)log.OpenPortsFound + " (<3)";
            if (log.AttackType == AttackType.BruteForce && log.AttemptCount <= 5 && log.LockoutTriggered == 0)
                return "BruteForce: AttemptCount=" + (int)log.AttemptCount + " (≤5), no lockout";
            if (log.AttackType == AttackType.DDoSFlood && log.RequestRate <= 500)
                return "DDoS: RequestRate=" + (int)log.RequestRate + " req/s (≤500)";
            if (log.AttackType == AttackType.SqlInjection && log.PatternFlag == 0 && log.WAFBypassed == 0 && log.AttemptCount <= 3)
                return "SQLi: no pattern flag, no WAF bypass, attempts=" + (int)log.AttemptCount;
            if (log.AttackType == AttackType.FileAccess && log.RestrictedAccess == 0)
                return "FileAccess: no restricted path hit — stealth enumeration";
            return "Event fell below all thresholds — advanced evasion.";
        }

        private MLEventData BuildInput(EventLog log) => new MLEventData
        {
            AttemptCount = log.AttemptCount,
            RequestRate = log.RequestRate,
            PortCount = log.PortCount,
            PatternFlag = log.PatternFlag,
            RestrictedAccess = log.RestrictedAccess,
            OpenPortsFound = log.OpenPortsFound,
            LockoutTriggered = log.LockoutTriggered,
            WAFBypassed = log.WAFBypassed,
            AttackDuration = log.AttackDuration
        };

        private float[] GetFeatureValues(EventLog log) => new float[] {
            log.AttemptCount, log.RequestRate, log.PortCount, log.PatternFlag,
            log.RestrictedAccess, log.OpenPortsFound, log.LockoutTriggered,
            log.WAFBypassed, log.AttackDuration };

        private EventLog CloneLog(EventLog src) => new EventLog
        {
            AttemptCount = src.AttemptCount,
            RequestRate = src.RequestRate,
            PortCount = src.PortCount,
            PatternFlag = src.PatternFlag,
            RestrictedAccess = src.RestrictedAccess,
            OpenPortsFound = src.OpenPortsFound,
            LockoutTriggered = src.LockoutTriggered,
            WAFBypassed = src.WAFBypassed,
            AttackDuration = src.AttackDuration
        };

        private void SetFeature(EventLog log, int index, float value)
        {
            if (index == 0) log.AttemptCount = value;
            else if (index == 1) log.RequestRate = value;
            else if (index == 2) log.PortCount = value;
            else if (index == 3) log.PatternFlag = value;
            else if (index == 4) log.RestrictedAccess = value;
            else if (index == 5) log.OpenPortsFound = value;
            else if (index == 6) log.LockoutTriggered = value;
            else if (index == 7) log.WAFBypassed = value;
            else if (index == 8) log.AttackDuration = value;
        }
    }
}