using CyberDuel.Models;

namespace CyberDuel.Attacks
{
    // Tüm saldırı türlerini simüle eden sınıf
    // Her saldırı tipi için ayrı metot yazdım
    public class AttackSimulator
    {
        private Random rng = new Random();

        // Rastgele kaynak IP üretir
        private string RandomIP()
        {
            return rng.Next(10, 200) + "." + rng.Next(0, 255) + "." +
                   rng.Next(0, 255) + "." + rng.Next(1, 254);
        }

        public EventLog DoPortScan(string targetName)
        {
            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.PortScan;

            // 10-60 arası port taranıyor
            log.PortCount = rng.Next(10, 60);
            log.AttemptCount = 1;
            log.RequestRate = 0;
            log.PatternFlag = 0;
            log.RestrictedAccess = 0;

            // 15'ten fazla port taranırsa saldırı sayılır
            if (log.PortCount > 15)
                log.IsMalicious = true;
            else
                log.IsMalicious = false;

            return log;
        }

        public EventLog DoBruteForce(string targetName)
        {
            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.BruteForce;

            // 2-20 arası giriş denemesi
            log.AttemptCount = rng.Next(2, 20);
            log.RequestRate = 0;
            log.PortCount = 0;
            log.PatternFlag = 0;
            log.RestrictedAccess = 0;

            // 5'ten fazla başarısız deneme → saldırı
            if (log.AttemptCount > 5)
                log.IsMalicious = true;
            else
                log.IsMalicious = false;

            return log;
        }

        public EventLog DoDDoS(string targetName)
        {
            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.DDoSFlood;

            // Saniyede gelen istek sayısı
            log.RequestRate = rng.Next(100, 1200);
            log.AttemptCount = 1;
            log.PortCount = 0;
            log.PatternFlag = 0;
            log.RestrictedAccess = 0;

            // 500 req/s üstü → saldırı
            if (log.RequestRate > 500)
                log.IsMalicious = true;
            else
                log.IsMalicious = false;

            return log;
        }

        public EventLog DoSqlInjection(string targetName)
        {
            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.SqlInjection;

            // %65 ihtimalle zararlı SQL payload gönderiliyor
            double chance = rng.NextDouble();
            if (chance > 0.35)
            {
                log.PatternFlag = 1;
                log.IsMalicious = true;
            }
            else
            {
                log.PatternFlag = 0;
                log.IsMalicious = false;
            }

            log.AttemptCount = 1;
            log.RequestRate = 0;
            log.PortCount = 0;
            log.RestrictedAccess = 0;

            return log;
        }

        public EventLog DoFileAccess(string targetName)
        {
            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.FileAccess;

            // %60 ihtimalle kısıtlı dosyaya erişim deneniyor
            double chance = rng.NextDouble();
            if (chance > 0.40)
            {
                log.RestrictedAccess = 1;
                log.IsMalicious = true;
            }
            else
            {
                log.RestrictedAccess = 0;
                log.IsMalicious = false;
            }

            log.AttemptCount = 1;
            log.RequestRate = 0;
            log.PortCount = 0;
            log.PatternFlag = 0;

            return log;
        }
    }
}