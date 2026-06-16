namespace CyberDuel.Models
{
    public class SessionRecord
    {
        public int MissionNumber { get; set; }
        public string TargetName { get; set; }
        public string Difficulty { get; set; }
        public int Score { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1 { get; set; }
        public double RuleOnlyF1 { get; set; }
        public double MLOnlyF1 { get; set; }
        public int TotalEvents { get; set; }
    }
}