using CyberDuel.Detection;
using CyberDuel.Models;

namespace CyberDuel.Evaluation
{
    public static class ReplayAnalyzer
    {
        public static void Run(MLDetector ml)
        {
            string[] files = Directory.GetFiles(".", "session_log_*.json");
            if (files.Length == 0) { Console.WriteLine("  No session log files found."); return; }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n  AVAILABLE SESSION LOGS:\n");
            for (int i = 0; i < files.Length; i++)
                Console.WriteLine("  [" + (i + 1) + "] " + Path.GetFileName(files[i]));
            Console.Write("\n  Select file (0 to cancel): ");
            Console.ResetColor();

            if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > files.Length) return;

            List<EventLog> events = ParseLogFile(files[choice - 1]);
            if (events.Count == 0) { Console.WriteLine("  Could not parse log file."); return; }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [ REPLAY ANALYSIS — " + Path.GetFileName(files[choice - 1]) + " ]");
            Console.WriteLine("  ┌────────────┬──────────────────┬──────────┬──────────┬──────────┐");
            Console.WriteLine("  │ Time       │ Attack           │ Orig ML  │ New ML   │ Changed? │");
            Console.WriteLine("  ├────────────┼──────────────────┼──────────┼──────────┼──────────┤");
            Console.ResetColor();

            int changed = 0;
            foreach (EventLog log in events)
            {
                float newProb; bool newResult = ml.Predict(log, out newProb);
                bool diff = log.MLDetected != newResult;
                if (diff) changed++;
                Console.ForegroundColor = diff ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
                Console.WriteLine("  │ " + log.Timestamp.ToString("HH:mm:ss").PadRight(10) +
                    " │ " + log.AttackType.ToString().PadRight(16) +
                    " │ " + (log.MLDetected ? "FLAGGED " : "CLEAN   ") +
                    " │ " + (newResult ? "FLAGGED " : "CLEAN   ") +
                    " │ " + (diff ? "YES      " : "NO       ") + " │");
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  └────────────┴──────────────────┴──────────┴──────────┴──────────┘");
            Console.WriteLine("  Changed decisions: " + changed + " / " + events.Count);
            Console.ResetColor();
            Console.Write("\n  Press Enter to continue...");
            Console.ReadLine();
        }

        private static List<EventLog> ParseLogFile(string path)
        {
            List<EventLog> result = new List<EventLog>();
            try
            {
                string[] lines = File.ReadAllLines(path);
                EventLog current = null;
                foreach (string line in lines)
                {
                    string t = line.Trim();
                    if (t.StartsWith("\"time\"")) { current = new EventLog(); if (TimeSpan.TryParse(ExtractStr(t), out TimeSpan ts)) current.Timestamp = DateTime.Today.Add(ts); }
                    else if (t.StartsWith("\"attack\"") && current != null) { if (Enum.TryParse<AttackType>(ExtractStr(t), out AttackType at)) current.AttackType = at; }
                    else if (t.StartsWith("\"attemptCount\"") && current != null) current.AttemptCount = ExtractFloat(t);
                    else if (t.StartsWith("\"requestRate\"") && current != null) current.RequestRate = ExtractFloat(t);
                    else if (t.StartsWith("\"portCount\"") && current != null) current.PortCount = ExtractFloat(t);
                    else if (t.StartsWith("\"patternFlag\"") && current != null) current.PatternFlag = ExtractFloat(t);
                    else if (t.StartsWith("\"restrictedAccess\"") && current != null) current.RestrictedAccess = ExtractFloat(t);
                    else if (t.StartsWith("\"openPortsFound\"") && current != null) current.OpenPortsFound = ExtractFloat(t);
                    else if (t.StartsWith("\"lockoutTriggered\"") && current != null) current.LockoutTriggered = ExtractFloat(t);
                    else if (t.StartsWith("\"wafBypassed\"") && current != null) current.WAFBypassed = ExtractFloat(t);
                    else if (t.StartsWith("\"attackDuration\"") && current != null) current.AttackDuration = ExtractFloat(t);
                    else if (t.StartsWith("\"originalMLDetected\"") && current != null) current.MLDetected = t.Contains("true");
                    else if ((t == "}," || t == "}") && current != null && current.AttackType != AttackType.Normal) { result.Add(current); current = null; }
                }
            }
            catch { }
            return result;
        }

        private static string ExtractStr(string line)
        {
            int first = line.IndexOf('"', line.IndexOf(':'));
            int second = line.IndexOf('"', first + 1);
            return (first < 0 || second < 0) ? "" : line.Substring(first + 1, second - first - 1);
        }

        private static float ExtractFloat(string line)
        {
            int colon = line.IndexOf(':');
            if (colon < 0) return 0;
            string raw = line.Substring(colon + 1).Trim().TrimEnd(',');
            return float.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0;
        }
    }
}