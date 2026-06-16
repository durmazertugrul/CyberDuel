using CyberDuel.Models;

namespace CyberDuel.Scoring
{
    public class RiskScorer
    {
        private double ruleWeight = 0.5;
        private double mlWeight = 0.5;

        public double Calculate(bool ruleResult, float mlProbability)
        {
            double ruleScore = ruleResult ? 1.0 : 0.0;
            return ruleWeight * ruleScore + mlWeight * mlProbability;
        }

        public ThreatLevel GetLevel(double score)
        {
            if (score < 0.25) return ThreatLevel.Low;
            else if (score < 0.50) return ThreatLevel.Moderate;
            else if (score < 0.75) return ThreatLevel.High;
            else return ThreatLevel.Critical;
        }
    }
}