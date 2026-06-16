namespace CyberDuel.Models
{
    public class SessionState
    {
        public List<int> DiscoveredOpenPorts = new List<int>();
        public bool ServerOffline = false;
        public bool AdminAccessGained = false;
        public string CompromisedUsername = "";
        public string CompromisedPassword = "";
        public bool DatabaseBreached = false;
        public string BreachedTable = "";
        public List<EventLog> AllEvents = new List<EventLog>();
        public int RuleTP = 0, RuleFP = 0, RuleFN = 0, RuleTN = 0;
        public int MLTP = 0, MLFP = 0, MLFN = 0, MLTN = 0;
        public int PreBlockedCount = 0;
        public HashSet<string> CompromisedTargets = new HashSet<string>();
        public List<(DateTime time, AttackType type)> RecentAttacks = new List<(DateTime, AttackType)>();

        public void RecordLayerResults(bool ruleResult, bool mlResult, bool actual)
        {
            if (actual && ruleResult) RuleTP++;
            else if (!actual && !ruleResult) RuleTN++;
            else if (!actual && ruleResult) RuleFP++;
            else RuleFN++;
            if (actual && mlResult) MLTP++;
            else if (!actual && !mlResult) MLTN++;
            else if (!actual && mlResult) MLFP++;
            else MLFN++;
        }

        public bool IsCoordinatedAttack()
        {
            DateTime cutoff = DateTime.Now.AddSeconds(-90);
            var recent = RecentAttacks.Where(a => a.time >= cutoff).ToList();
            return recent.Select(a => a.type).Distinct().Count() >= 3;
        }

        public bool IsRepeatedPattern()
        {
            if (RecentAttacks.Count < 3) return false;
            var last3 = RecentAttacks.TakeLast(3).Select(a => a.type).ToList();
            return last3[0] == last3[1] && last3[1] == last3[2];
        }

        public void RecordAttack(AttackType type)
        {
            RecentAttacks.Add((DateTime.Now, type));
            if (RecentAttacks.Count > 10) RecentAttacks.RemoveAt(0);
        }

        public bool HasLateralAccess(string targetName) =>
            CompromisedTargets.Count > 0 && !CompromisedTargets.Contains(targetName);

        public void PrintLayerComparison()
        {
            double ruleP = (RuleTP + RuleFP) == 0 ? 0 : (double)RuleTP / (RuleTP + RuleFP);
            double ruleR = (RuleTP + RuleFN) == 0 ? 0 : (double)RuleTP / (RuleTP + RuleFN);
            double ruleF1 = (ruleP + ruleR) == 0 ? 0 : 2 * ruleP * ruleR / (ruleP + ruleR);
            double mlP = (MLTP + MLFP) == 0 ? 0 : (double)MLTP / (MLTP + MLFP);
            double mlR = (MLTP + MLFN) == 0 ? 0 : (double)MLTP / (MLTP + MLFN);
            double mlF1 = (mlP + mlR) == 0 ? 0 : 2 * mlP * mlR / (mlP + mlR);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [ DETECTION LAYER COMPARISON ]");
            Console.WriteLine("  ┌──────────────┬───────────┬────────┬────────┐");
            Console.WriteLine("  │ Layer        │ Precision │ Recall │ F1     │");
            Console.WriteLine("  ├──────────────┼───────────┼────────┼────────┤");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  │ Rule Only    │ " + ruleP.ToString("F2").PadLeft(9) + " │ " + ruleR.ToString("F2").PadLeft(6) + " │ " + ruleF1.ToString("F2").PadLeft(6) + " │");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("  │ ML Only      │ " + mlP.ToString("F2").PadLeft(9) + " │ " + mlR.ToString("F2").PadLeft(6) + " │ " + mlF1.ToString("F2").PadLeft(6) + " │");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  │ Hybrid       │ (see IDS metrics above)       │");
            Console.WriteLine("  └──────────────┴───────────┴────────┴────────┘");
            Console.ResetColor();
            if (PreBlockedCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("  Pre-blocked (IP ban): " + PreBlockedCount + " attack(s) — excluded from rule/ML metrics");
                Console.ResetColor();
            }
        }

        public (double f1Rule, double f1ML) GetLayerF1s()
        {
            double ruleP = (RuleTP + RuleFP) == 0 ? 0 : (double)RuleTP / (RuleTP + RuleFP);
            double ruleR = (RuleTP + RuleFN) == 0 ? 0 : (double)RuleTP / (RuleTP + RuleFN);
            double ruleF1 = (ruleP + ruleR) == 0 ? 0 : 2 * ruleP * ruleR / (ruleP + ruleR);
            double mlP = (MLTP + MLFP) == 0 ? 0 : (double)MLTP / (MLTP + MLFP);
            double mlR = (MLTP + MLFN) == 0 ? 0 : (double)MLTP / (MLTP + MLFN);
            double mlF1 = (mlP + mlR) == 0 ? 0 : 2 * mlP * mlR / (mlP + mlR);
            return (ruleF1, mlF1);
        }

        public void Reset()
        {
            DiscoveredOpenPorts.Clear(); ServerOffline = false;
            AdminAccessGained = false; CompromisedUsername = ""; CompromisedPassword = "";
            DatabaseBreached = false; BreachedTable = ""; AllEvents.Clear();
            RuleTP = RuleFP = RuleFN = RuleTN = MLTP = MLFP = MLFN = MLTN = PreBlockedCount = 0;
            RecentAttacks.Clear();
        }
    }
}