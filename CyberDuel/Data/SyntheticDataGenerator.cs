using CyberDuel.Models;

namespace CyberDuel.Data
{
    // ML modelini eğitmek için sahte log verisi üretimi
    public class SyntheticDataGenerator
    {
        private Random rng = new Random();

        public List<EventLog> Generate(int count)
        {
            List<EventLog> result = new List<EventLog>();

            // Yarısı normal trafik, yarısı saldırı
            int half = count / 2;

            for (int i = 0; i < half; i++)
            {
                result.Add(MakeNormal());
            }

            // Her saldırı türünden eşit sayıda örnek üret
            int perType = half / 5;
            for (int i = 0; i < perType; i++) result.Add(MakePortScan());
            for (int i = 0; i < perType; i++) result.Add(MakeBruteForce());
            for (int i = 0; i < perType; i++) result.Add(MakeDDoS());
            for (int i = 0; i < perType; i++) result.Add(MakeSqlInjection());
            for (int i = 0; i < perType; i++) result.Add(MakeFileAccess());

            // Listeyi karıştır 
            result = result.OrderBy(x => rng.Next()).ToList();

            return result;
        }

        public void SaveToCsv(List<EventLog> logs, string path)
        {
            List<string> lines = new List<string>();
            lines.Add("AttemptCount,RequestRate,PortCount,PatternFlag,RestrictedAccess,IsMalicious");

            foreach (EventLog log in logs)
            {
                string label = log.IsMalicious ? "true" : "false";
                lines.Add(log.AttemptCount + "," + log.RequestRate + "," +
                          log.PortCount + "," + log.PatternFlag + "," +
                          log.RestrictedAccess + "," + label);
            }

            File.WriteAllLines(path, lines);
        }

        // Normal trafik örneği 
        private EventLog MakeNormal()
        {
            return new EventLog
            {
                AttackType = AttackType.Normal,
                AttemptCount = rng.Next(1, 4),
                RequestRate = rng.Next(1, 150),
                PortCount = rng.Next(1, 10),
                PatternFlag = 0,
                RestrictedAccess = 0,
                IsMalicious = false
            };
        }

        // Port scan saldırısı örneği
        private EventLog MakePortScan()
        {
            return new EventLog
            {
                AttackType = AttackType.PortScan,
                PortCount = rng.Next(20, 65),
                AttemptCount = 1,
                RequestRate = 0,
                PatternFlag = 0,
                RestrictedAccess = 0,
                IsMalicious = true
            };
        }

        // Brute force örneği
        private EventLog MakeBruteForce()
        {
            return new EventLog
            {
                AttackType = AttackType.BruteForce,
                AttemptCount = rng.Next(8, 25),
                RequestRate = 0,
                PortCount = 0,
                PatternFlag = 0,
                RestrictedAccess = 0,
                IsMalicious = true
            };
        }

        // DDoS flood örneği
        private EventLog MakeDDoS()
        {
            return new EventLog
            {
                AttackType = AttackType.DDoSFlood,
                RequestRate = rng.Next(600, 1500),
                AttemptCount = 1,
                PortCount = 0,
                PatternFlag = 0,
                RestrictedAccess = 0,
                IsMalicious = true
            };
        }

        // SQL injection örneği
        private EventLog MakeSqlInjection()
        {
            return new EventLog
            {
                AttackType = AttackType.SqlInjection,
                PatternFlag = 1,
                AttemptCount = 1,
                RequestRate = 0,
                PortCount = 0,
                RestrictedAccess = 0,
                IsMalicious = true
            };
        }

        // Yetkisiz dosya erişimi örneği
        private EventLog MakeFileAccess()
        {
            return new EventLog
            {
                AttackType = AttackType.FileAccess,
                RestrictedAccess = 1,
                AttemptCount = 1,
                RequestRate = 0,
                PortCount = 0,
                PatternFlag = 0,
                IsMalicious = true
            };
        }
    }
}