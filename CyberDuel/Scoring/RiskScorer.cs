using CyberDuel.Models;

namespace CyberDuel.Scoring
{
    // Her olaya bir risk skoru atar ve tehdit seviyesini belirler
    public class RiskScorer
    {
        // Her iki katmana eşit ağırlık
        private double ruleWeight = 0.5;
        private double mlWeight = 0.5;

        public double Calculate(bool ruleResult, float mlProbability)
        {
            double ruleScore = 0.0;
            if (ruleResult == true)
                ruleScore = 1.0;

            double score = ruleWeight * ruleScore + mlWeight * mlProbability;
            return score;
        }

        public ThreatLevel GetLevel(double score)
        {
            // Skora göre tehdit seviyesi belirleme
            if (score < 0.25)
                return ThreatLevel.Low;
            else if (score < 0.50)
                return ThreatLevel.Moderate;
            else if (score < 0.75)
                return ThreatLevel.High;
            else
                return ThreatLevel.Critical;
        }
    }
}