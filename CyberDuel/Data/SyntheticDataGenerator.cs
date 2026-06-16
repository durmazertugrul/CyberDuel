using CyberDuel.Models;

namespace CyberDuel.Data
{
    public class SyntheticDataGenerator
    {
        private Random rng = new Random();

        public List<EventLog> Generate(int count)
        {
            List<EventLog> result = new List<EventLog>();
            int half = count / 2;

            // Normal trafik: %70 açıkça normal, %30 borderline
            int normalClear = (int)(half * 0.70);
            for (int i = 0; i < normalClear; i++) result.Add(MakeNormal());
            int normalBorderline = half - normalClear;
            for (int i = 0; i < normalBorderline; i++) result.Add(MakeBorderlineNormal());

            // Saldırılar: %70 açık saldırı, %30 evasive (rule engine kaçırır, ML yakalar)
            int perType = half / 5;
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakePortScan(false));
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakeBruteForce(false));
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakeDDoS(false));
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakeSqlInjection(false));
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakeFileAccess(false));
            int evasive = perType - (int)(perType * 0.70);
            for (int i = 0; i < evasive; i++) result.Add(MakePortScan(true));
            for (int i = 0; i < evasive; i++) result.Add(MakeBruteForce(true));
            for (int i = 0; i < evasive; i++) result.Add(MakeDDoS(true));
            for (int i = 0; i < evasive; i++) result.Add(MakeSqlInjection(true));
            for (int i = 0; i < evasive; i++) result.Add(MakeFileAccess(true));

            return result.OrderBy(x => rng.Next()).ToList();
        }

        public void SaveToCsv(List<EventLog> logs, string path)
        {
            List<string> lines = new List<string>();
            lines.Add("AttemptCount,RequestRate,PortCount,PatternFlag,RestrictedAccess," +
                      "OpenPortsFound,LockoutTriggered,WAFBypassed,AttackDuration,IsMalicious");
            foreach (EventLog log in logs)
            {
                string label = log.IsMalicious ? "true" : "false";
                lines.Add(log.AttemptCount + "," + log.RequestRate + "," + log.PortCount + "," +
                          log.PatternFlag + "," + log.RestrictedAccess + "," + log.OpenPortsFound + "," +
                          log.LockoutTriggered + "," + log.WAFBypassed + "," + log.AttackDuration + "," + label);
            }
            File.WriteAllLines(path, lines);
        }

        private EventLog MakeNormal() => new EventLog
        {
            AttackType = AttackType.Normal,
            AttemptCount = rng.Next(1, 3),
            RequestRate = rng.Next(1, 80),
            PortCount = rng.Next(1, 5),
            PatternFlag = 0,
            RestrictedAccess = 0,
            OpenPortsFound = rng.Next(0, 2),
            LockoutTriggered = 0,
            WAFBypassed = 0,
            AttackDuration = (float)(rng.NextDouble() * 1.5),
            IsMalicious = false
        };

        private EventLog MakeBorderlineNormal() => new EventLog
        {
            AttackType = AttackType.Normal,
            AttemptCount = rng.Next(3, 6),
            RequestRate = rng.Next(80, 200),
            PortCount = rng.Next(5, 14),
            PatternFlag = 0,
            RestrictedAccess = 0,
            OpenPortsFound = rng.Next(1, 3),
            LockoutTriggered = 0,
            WAFBypassed = 0,
            AttackDuration = (float)(rng.NextDouble() * 3 + 1),
            IsMalicious = false
        };

        private EventLog MakePortScan(bool evasive) => new EventLog
        {
            AttackType = AttackType.PortScan,
            PortCount = evasive ? rng.Next(10, 16) : rng.Next(20, 65),
            OpenPortsFound = evasive ? rng.Next(2, 4) : rng.Next(4, 9),
            AttemptCount = 1,
            RequestRate = 0,
            PatternFlag = 0,
            RestrictedAccess = 0,
            LockoutTriggered = 0,
            WAFBypassed = 0,
            AttackDuration = (float)(rng.NextDouble() * 5 + 2),
            IsMalicious = true
        };

        private EventLog MakeBruteForce(bool evasive)
        {
            int attempts = evasive ? rng.Next(4, 7) : rng.Next(10, 30);
            return new EventLog
            {
                AttackType = AttackType.BruteForce,
                AttemptCount = attempts,
                LockoutTriggered = (!evasive && attempts > 10) ? 1 : 0,
                RequestRate = 0,
                PortCount = 0,
                PatternFlag = 0,
                RestrictedAccess = 0,
                OpenPortsFound = 0,
                WAFBypassed = 0,
                AttackDuration = (float)(rng.NextDouble() * 8 + 2),
                IsMalicious = true
            };
        }

        private EventLog MakeDDoS(bool evasive) => new EventLog
        {
            AttackType = AttackType.DDoSFlood,
            RequestRate = evasive ? (float)rng.Next(300, 501) : (float)rng.Next(600, 1800),
            AttackDuration = evasive ? (float)(rng.NextDouble() * 5 + 4) : (float)(rng.NextDouble() * 10 + 5),
            AttemptCount = 1,
            PortCount = 0,
            PatternFlag = 0,
            RestrictedAccess = 0,
            OpenPortsFound = 0,
            LockoutTriggered = 0,
            WAFBypassed = 0,
            IsMalicious = true
        };

        private EventLog MakeSqlInjection(bool evasive) => new EventLog
        {
            AttackType = AttackType.SqlInjection,
            PatternFlag = evasive ? 0 : 1,
            WAFBypassed = (!evasive && rng.NextDouble() < 0.4) ? 1 : 0,
            AttemptCount = evasive ? rng.Next(4, 8) : rng.Next(1, 5),
            RequestRate = 0,
            PortCount = 0,
            RestrictedAccess = 0,
            OpenPortsFound = 0,
            LockoutTriggered = 0,
            AttackDuration = (float)(rng.NextDouble() * 4 + 1),
            IsMalicious = true
        };

        private EventLog MakeFileAccess(bool evasive) => new EventLog
        {
            AttackType = AttackType.FileAccess,
            RestrictedAccess = evasive ? 0 : 1,
            AttemptCount = evasive ? rng.Next(6, 12) : rng.Next(3, 8),
            AttackDuration = (float)(rng.NextDouble() * 3 + 1),
            RequestRate = 0,
            PortCount = 0,
            PatternFlag = 0,
            OpenPortsFound = 0,
            LockoutTriggered = 0,
            WAFBypassed = 0,
            IsMalicious = true
        };
    }
}