using CyberDuel.Models;

namespace CyberDuel.Detection
{
    // Kural tabanlı tespit: önceden tanımlı eşik değerlerine göre karar verir
    public class RuleEngine
    {
        // Eşik sabitleri
        private const int PORT_THRESHOLD = 15;
        private const int ATTEMPT_THRESHOLD = 5;
        private const double RATE_THRESHOLD = 500.0;

        public bool Check(EventLog log)
        {
            bool detected = false;

            if (log.AttackType == AttackType.PortScan)
            {
                // 15'ten fazla port tarandıysa şüpheli
                if (log.PortCount > PORT_THRESHOLD)
                    detected = true;
            }
            else if (log.AttackType == AttackType.BruteForce)
            {
                // 5'ten fazla başarısız giriş denemesi varsa şüpheli
                if (log.AttemptCount > ATTEMPT_THRESHOLD)
                    detected = true;
            }
            else if (log.AttackType == AttackType.DDoSFlood)
            {
                // Saniyede 500'den fazla istek geliyorsa şüpheli
                if (log.RequestRate > RATE_THRESHOLD)
                    detected = true;
            }
            else if (log.AttackType == AttackType.SqlInjection)
            {
                // SQL payload eşleşmesi varsa zararlı
                if (log.PatternFlag == 1)
                    detected = true;
            }
            else if (log.AttackType == AttackType.FileAccess)
            {
                // Kısıtlı dosyaya erişim girişimi varsa zararlı
                if (log.RestrictedAccess == 1)
                    detected = true;
            }

            return detected;
        }
    }
}