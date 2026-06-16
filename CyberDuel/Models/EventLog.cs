namespace CyberDuel.Models
{
    public class EventLog
    {
        public DateTime Timestamp { get; set; }
        public string SourceIP { get; set; }
        public string TargetSystem { get; set; }
        public AttackType AttackType { get; set; }
        public float AttemptCount { get; set; }
        public float RequestRate { get; set; }
        public float PortCount { get; set; }
        public float PatternFlag { get; set; }
        public float RestrictedAccess { get; set; }
        public float OpenPortsFound { get; set; }
        public float LockoutTriggered { get; set; }
        public float WAFBypassed { get; set; }
        public float AttackDuration { get; set; }
        public bool AttackSuccess { get; set; }
        public bool IsMalicious { get; set; }
        public bool RuleDetected { get; set; }
        public bool MLDetected { get; set; }
        public float MLProbability { get; set; }
        public double RiskScore { get; set; }
        public ThreatLevel ThreatLevel { get; set; }
    }
}