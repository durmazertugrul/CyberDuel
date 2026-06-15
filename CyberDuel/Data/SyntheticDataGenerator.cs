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
            for (int i = 0; i < half; i++)
                result.Add(MakeNormal());

            int perType = half / 5;
            for (int i = 0; i < perType; i++) result.Add(MakePortScan());
            for (int i = 0; i < perType; i++) result.Add(MakeBruteForce());
            for (int i = 0; i < perType; i++) result.Add(MakeDDoS());
            for (int i = 0; i < perType; i++) result.Add(MakeSqlInjection());
            for (int i = 0; i < perType; i++) result.Add(MakeFileAccess());

            result = result.OrderBy(x => rng.Next()).ToList();
            return result;
        }

        public void SaveToCsv(List<EventLog> logs, string path)
        {
            List<string> lines = new List<string>();
            lines.Add("AttemptCount,RequestRate,PortCount,PatternFlag,RestrictedAccess," +
                      "OpenPortsFound,LockoutTriggered,WAFBypassed,AttackDuration,IsMalicious");

            foreach (EventLog log in logs)
            {
                string label = log.IsMalicious ? "true" : "false";
                lines.Add(log.AttemptCount + "," + log.RequestRate + "," +
                          log.PortCount + "," + log.PatternFlag + "," +
                          log.RestrictedAccess + "," + log.OpenPortsFound + "," +
                          log.LockoutTriggered + "," + log.WAFBypassed + "," +
                          log.AttackDuration + "," + label);
            }

            File.WriteAllLines(path, lines);
        }

        private EventLog MakeNormal()
        {
            return new EventLog
            {
                AttackType = AttackType.Normal,
                AttemptCount = rng.Next(1, 3),
                RequestRate = rng.Next(1, 100),
                PortCount = rng.Next(1, 5),
                PatternFlag = 0,
                RestrictedAccess = 0,
                OpenPortsFound = rng.Next(0, 2),
                LockoutTriggered = 0,
                WAFBypassed = 0,
                AttackDuration = (float)(rng.NextDouble() * 2),
                IsMalicious = false
            };
        }

        private EventLog MakePortScan()
        {
            int open = rng.Next(3, 8);
            return new EventLog
            {
                AttackType = AttackType.PortScan,
                PortCount = rng.Next(20, 65),
                OpenPortsFound = open,
                AttemptCount = 1,
                RequestRate = 0,
                PatternFlag = 0,
                RestrictedAccess = 0,
                LockoutTriggered = 0,
                WAFBypassed = 0,
                AttackDuration = (float)(rng.NextDouble() * 5 + 2),
                IsMalicious = true
            };
        }

        private EventLog MakeBruteForce()
        {
            int attempts = rng.Next(8, 30);
            bool lockout = attempts > 10;
            return new EventLog
            {
                AttackType = AttackType.BruteForce,
                AttemptCount = attempts,
                LockoutTriggered = lockout ? 1 : 0,
                RequestRate = 0,
                PortCount = 0,
                PatternFlag = 0,
                RestrictedAccess = 0,
                OpenPortsFound = 0,
                WAFBypassed = 0,
                AttackDuration = (float)(rng.NextDouble() * 8 + 3),
                IsMalicious = true
            };
        }

        private EventLog MakeDDoS()
        {
            return new EventLog
            {
                AttackType = AttackType.DDoSFlood,
                RequestRate = rng.Next(600, 1800),
                AttackDuration = (float)(rng.NextDouble() * 10 + 5),
                AttemptCount = 1,
                PortCount = 0,
                PatternFlag = 0,
                RestrictedAccess = 0,
                OpenPortsFound = 0,
                LockoutTriggered = 0,
                WAFBypassed = 0,
                IsMalicious = true
            };
        }

        private EventLog MakeSqlInjection()
        {
            bool wafBypass = rng.NextDouble() < 0.4;
            return new EventLog
            {
                AttackType = AttackType.SqlInjection,
                PatternFlag = 1,
                WAFBypassed = wafBypass ? 1 : 0,
                AttemptCount = rng.Next(2, 8),
                RequestRate = 0,
                PortCount = 0,
                RestrictedAccess = 0,
                OpenPortsFound = 0,
                LockoutTriggered = 0,
                AttackDuration = (float)(rng.NextDouble() * 4 + 1),
                IsMalicious = true
            };
        }

        private EventLog MakeFileAccess()
        {
            return new EventLog
            {
                AttackType = AttackType.FileAccess,
                RestrictedAccess = 1,
                AttemptCount = rng.Next(3, 12),
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
}