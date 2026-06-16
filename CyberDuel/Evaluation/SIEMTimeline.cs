using CyberDuel.Models;

namespace CyberDuel.Evaluation
{
    public static class SIEMTimeline
    {
        public static void Print(List<EventLog> events, string targetName)
        {
            if (events.Count == 0) return;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n  ┌─────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │  SIEM EVENT TIMELINE — " + targetName.PadRight(41) + "│");
            Console.WriteLine("  ├──────────┬──────────────────┬───────────────────┬───────┬────────┤");
            Console.WriteLine("  │ Time     │ Attack Type      │ Source IP         │ Level │ Status │");
            Console.WriteLine("  ├──────────┼──────────────────┼───────────────────┼───────┼────────┤");
            Console.ResetColor();
            foreach (EventLog e in events)
            {
                bool detected = e.RuleDetected || e.MLDetected;
                ConsoleColor c = e.ThreatLevel == ThreatLevel.Critical ? ConsoleColor.Red :
                                 e.ThreatLevel == ThreatLevel.High ? ConsoleColor.DarkYellow :
                                 e.ThreatLevel == ThreatLevel.Moderate ? ConsoleColor.Yellow :
                                 detected ? ConsoleColor.Green : ConsoleColor.DarkGray;
                Console.ForegroundColor = c;
                Console.WriteLine("  │ " + e.Timestamp.ToString("HH:mm:ss").PadRight(8) +
                    " │ " + e.AttackType.ToString().PadRight(16) +
                    " │ " + e.SourceIP.PadRight(17) +
                    " │ " + e.ThreatLevel.ToString().PadRight(5) +
                    " │ " + (detected ? "ALERT " : "CLEAN ") + " │");
                Console.ResetColor();
            }
            int det = events.Count(e => e.RuleDetected || e.MLDetected);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ├──────────┴──────────────────┴───────────────────┴───────┴────────┤");
            Console.WriteLine("  │  Total: " + events.Count.ToString().PadLeft(3) + "  │  Detected: " + det.ToString().PadLeft(3) + "  │  Missed: " + (events.Count - det).ToString().PadLeft(3) + "                              │");
            Console.WriteLine("  └─────────────────────────────────────────────────────────────────┘");
            Console.ResetColor();
        }
    }
}