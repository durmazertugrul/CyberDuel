namespace CyberDuel.Models
{
    // Her saldırı olayını temsil eden sınıf
    public class EventLog
    {
        public DateTime Timestamp { get; set; }
        public string SourceIP { get; set; }
        public string TargetSystem { get; set; }
        public AttackType AttackType { get; set; }

        // Saldırı türüne göre doldurulan özellik alanları
        public float AttemptCount { get; set; }
        public float RequestRate { get; set; }
        public float PortCount { get; set; }
        public float PatternFlag { get; set; }
        public float RestrictedAccess { get; set; }

        // Tespit katmanlarının sonuçları
        public bool IsMalicious { get; set; }
        public bool RuleDetected { get; set; }
        public bool MLDetected { get; set; }
        public float MLProbability { get; set; }

        public double RiskScore { get; set; }
        public ThreatLevel ThreatLevel { get; set; }
    }
}