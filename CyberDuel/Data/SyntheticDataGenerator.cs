using CyberDuel.Models;

namespace CyberDuel.Data
{
    // ML modelinin rule engine'den bağımsız öğrenmesini sağlamak için
    // borderline (eşik altı ama şüpheli) ve normal-görünümlü örnekler eklendi
    public class SyntheticDataGenerator
    {
        private Random rng = new Random();

        public List<EventLog> Generate(int count)
        {
            List<EventLog> result = new List<EventLog>();

            int half = count / 2;

            // Normal trafik — açıkça normal
            int normalClear = (int)(half * 0.70);
            for (int i = 0; i < normalClear; i++)
                result.Add(MakeNormal());

            // Normal görünümlü ama hafif anormal (ML öğrensin diye)
            int normalBorderline = half - normalClear;
            for (int i = 0; i < normalBorderline; i++)
                result.Add(MakeBorderlineNormal());

            // Saldırı örnekleri
            int perType = half / 5;

            // Açıkça saldırı — hem rule hem ML yakalar
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakePortScan(evasive: false));
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakeBruteForce(evasive: false));
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakeDDoS(evasive: false));
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakeSqlInjection(evasive: false));
            for (int i = 0; i < (int)(perType * 0.70); i++) result.Add(MakeFileAccess(evasive: false));

            // Evasive saldırı — rule engine kaçırır ama ML yakalamalı
            int evasive = perType - (int)(perType * 0.70);
            for (int i = 0; i < evasive; i++) result.Add(MakePortScan(evasive: true));
            for (int i = 0; i < evasive; i++) result.Add(MakeBruteForce(evasive: true));
            for (int i = 0; i < evasive; i++) result.Add(MakeDDoS(evasive: true));
            for (int i = 0; i < evasive; i++) result.Add(MakeSqlInjection(evasive: true));
            for (int i = 0; i < evasive; i++) result.Add(MakeFileAccess(evasive: true));

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

        // ── Normal örnekler ───────────────────────────────────────────────────

        private EventLog MakeNormal()
        {
            return new EventLog
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
        }

        // Hafif anormal ama gerçekten zararsız — ML'in yanlış öğrenmemesi için
        private EventLog MakeBorderlineNormal()
        {
            return new EventLog
            {
                AttackType = AttackType.Normal,
                AttemptCount = rng.Next(3, 6),   // eşiğe yakın ama altında
                RequestRate = rng.Next(80, 200),
                PortCount = rng.Next(5, 14),   // 15'in altında
                PatternFlag = 0,
                RestrictedAccess = 0,
                OpenPortsFound = rng.Next(1, 3),
                LockoutTriggered = 0,
                WAFBypassed = 0,
                AttackDuration = (float)(rng.NextDouble() * 3 + 1),
                IsMalicious = false
            };
        }

        // ── Saldırı örnekleri ─────────────────────────────────────────────────

        private EventLog MakePortScan(bool evasive)
        {
            // Evasive: az port tara, rule engine kaçırır ama kombinasyon şüpheli
            int ports = evasive ? rng.Next(10, 16) : rng.Next(20, 65);
            int open = evasive ? rng.Next(2, 4) : rng.Next(4, 9);

            return new EventLog
            {
                AttackType = AttackType.PortScan,
                PortCount = ports,
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

        private EventLog MakeBruteForce(bool evasive)
        {
            // Evasive: az deneme, lockout yok ama kombinasyon şüpheli
            int attempts = evasive ? rng.Next(4, 7) : rng.Next(10, 30);
            bool lockout = !evasive && attempts > 10;

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
                AttackDuration = (float)(rng.NextDouble() * 8 + 2),
                IsMalicious = true
            };
        }

        private EventLog MakeDDoS(bool evasive)
        {
            // Evasive: hız eşiğin altında ama süre uzun (rule engine kaçırır)
            float rate = evasive
                ? (float)rng.Next(300, 501)
                : (float)rng.Next(600, 1800);
            float duration = evasive
                ? (float)(rng.NextDouble() * 5 + 4)  // uzun süre
                : (float)(rng.NextDouble() * 10 + 5);

            return new EventLog
            {
                AttackType = AttackType.DDoSFlood,
                RequestRate = rate,
                AttackDuration = duration,
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

        private EventLog MakeSqlInjection(bool evasive)
        {
            // Evasive: WAF bypass yok, pattern flag 0 ama birden fazla deneme
            bool pattern = !evasive;
            bool waf = !evasive && rng.NextDouble() < 0.4;
            int attempts = evasive ? rng.Next(4, 8) : rng.Next(1, 5);

            return new EventLog
            {
                AttackType = AttackType.SqlInjection,
                PatternFlag = pattern ? 1 : 0,
                WAFBypassed = waf ? 1 : 0,
                AttemptCount = attempts,
                RequestRate = 0,
                PortCount = 0,
                RestrictedAccess = 0,
                OpenPortsFound = 0,
                LockoutTriggered = 0,
                AttackDuration = (float)(rng.NextDouble() * 4 + 1),
                IsMalicious = true
            };
        }

        private EventLog MakeFileAccess(bool evasive)
        {
            // Evasive: restricted flag yok ama çok fazla dosya denemesi
            int attempts = evasive ? rng.Next(6, 12) : rng.Next(3, 8);
            int restricted = evasive ? 0 : 1;

            return new EventLog
            {
                AttackType = AttackType.FileAccess,
                RestrictedAccess = restricted,
                AttemptCount = attempts,
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