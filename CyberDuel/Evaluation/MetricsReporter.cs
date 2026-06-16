using CyberDuel.Models;

namespace CyberDuel.Evaluation
{
    public class MetricsReporter
    {
        private int tp = 0, tn = 0, fp = 0, fn = 0;
        private Dictionary<AttackType, (int tp, int fp, int fn, int tn)> perType =
            new Dictionary<AttackType, (int, int, int, int)>();

        public void Add(bool predicted, bool actual) => Add(predicted, actual, AttackType.Normal);

        public void Add(bool predicted, bool actual, AttackType type)
        {
            if (actual && predicted) tp++;
            else if (!actual && !predicted) tn++;
            else if (!actual && predicted) fp++;
            else fn++;

            if (!perType.ContainsKey(type)) perType[type] = (0, 0, 0, 0);
            var (ptp, pfp, pfn, ptn) = perType[type];
            if (actual && predicted) ptp++;
            else if (!actual && !predicted) ptn++;
            else if (!actual && predicted) pfp++;
            else pfn++;
            perType[type] = (ptp, pfp, pfn, ptn);
        }

        public double GetPrecision() => (tp + fp) == 0 ? 0 : (double)tp / (tp + fp);
        public double GetRecall() => (tp + fn) == 0 ? 0 : (double)tp / (tp + fn);
        public double GetF1()
        {
            double p = GetPrecision(), r = GetRecall();
            return (p + r) == 0 ? 0 : 2 * p * r / (p + r);
        }
        public int Total() => tp + tn + fp + fn;

        public void PrintMatrix()
        {
            Console.WriteLine();
            Console.WriteLine("  [ CONFUSION MATRIX ]");
            Console.WriteLine("  TP (Correctly Detected) : " + tp);
            Console.WriteLine("  TN (Correctly Ignored)  : " + tn);
            Console.WriteLine("  FP (False Alarm)        : " + fp);
            Console.WriteLine("  FN (Missed Attack)      : " + fn);
        }

        public void PrintPerTypeMetrics()
        {
            if (perType.Count == 0) return;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [ PER-ATTACK-TYPE METRICS ]");
            Console.WriteLine("  ┌──────────────────────┬───────────┬────────┬────────┐");
            Console.WriteLine("  │ Attack Type          │ Precision │ Recall │ F1     │");
            Console.WriteLine("  ├──────────────────────┼───────────┼────────┼────────┤");
            foreach (var entry in perType)
            {
                if (entry.Key == AttackType.Normal) continue;
                var (ptp, pfp, pfn, ptn) = entry.Value;
                double prec = (ptp + pfp) == 0 ? 0 : (double)ptp / (ptp + pfp);
                double rec = (ptp + pfn) == 0 ? 0 : (double)ptp / (ptp + pfn);
                double f1 = (prec + rec) == 0 ? 0 : 2 * prec * rec / (prec + rec);
                Console.ForegroundColor = f1 >= 0.80 ? ConsoleColor.Green : f1 >= 0.60 ? ConsoleColor.Yellow : ConsoleColor.Red;
                Console.WriteLine("  │ " + entry.Key.ToString().PadRight(20) + " │ " + prec.ToString("F2").PadLeft(9) + " │ " + rec.ToString("F2").PadLeft(6) + " │ " + f1.ToString("F2").PadLeft(6) + " │");
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  └──────────────────────┴───────────┴────────┴────────┘");
            Console.ResetColor();
        }

        public void PrintDetectionChart()
        {
            if (perType.Count == 0) return;
            int barWidth = 20;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [ DETECTION RATE BY ATTACK TYPE ]");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ──────────────────────────────────────────────────────────");
            Console.ResetColor();
            foreach (var entry in perType)
            {
                if (entry.Key == AttackType.Normal) continue;
                var (ptp, pfp, pfn, ptn) = entry.Value;
                int total = ptp + pfn;
                double rate = total == 0 ? 0 : (double)ptp / total;
                int filled = (int)(rate * barWidth);
                string bar = "[" + new string('█', filled) + new string('-', barWidth - filled) + "]";
                string label = entry.Key.ToString().PadRight(18);
                string pct = ((int)(rate * 100)).ToString().PadLeft(3) + "%";
                Console.ForegroundColor = rate >= 0.80 ? ConsoleColor.Green : rate >= 0.60 ? ConsoleColor.Yellow : ConsoleColor.Red;
                Console.WriteLine("  " + label + " " + bar + " " + pct);
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ──────────────────────────────────────────────────────────");
            Console.ResetColor();
        }

        public void PrintAll()
        {
            PrintMatrix();
            Console.WriteLine();
            Console.WriteLine("  [ IDS PERFORMANCE METRICS ]");
            Console.WriteLine("  Precision  : " + GetPrecision().ToString("F2"));
            Console.WriteLine("  Recall     : " + GetRecall().ToString("F2"));
            Console.WriteLine("  F1-Score   : " + GetF1().ToString("F2"));
            Console.WriteLine("  Total      : " + Total());
            PrintPerTypeMetrics();
            PrintDetectionChart();
        }

        public void Reset() { tp = tn = fp = fn = 0; perType.Clear(); }
    }
}