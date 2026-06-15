using CyberDuel.Models;

namespace CyberDuel.Detection
{
    // Kural tabanlı tespit — çoklu koşul kombinasyonları
    public class RuleEngine
    {
        private const int PORT_THRESHOLD = 15;
        private const int ATTEMPT_THRESHOLD = 5;
        private const double RATE_THRESHOLD = 500.0;
        private const int LOCKOUT_WEIGHT = 3;

        public bool Check(EventLog log)
        {
            bool detected = false;

            if (log.AttackType == AttackType.PortScan)
            {
                // Hem toplam port sayısı hem de bulunan açık port sayısına bak
                if (log.PortCount > PORT_THRESHOLD)
                    detected = true;
                if (log.OpenPortsFound >= 3)
                    detected = true;
            }
            else if (log.AttackType == AttackType.BruteForce)
            {
                // Deneme sayısı veya hesap kilitleme tetiklenmesi
                if (log.AttemptCount > ATTEMPT_THRESHOLD)
                    detected = true;
                if (log.LockoutTriggered == 1)
                    detected = true;
                // Hem yüksek deneme hem lockout varsa kesin tespit
                if (log.AttemptCount > ATTEMPT_THRESHOLD && log.LockoutTriggered == 1)
                    detected = true;
            }
            else if (log.AttackType == AttackType.DDoSFlood)
            {
                // İstek hızı ve süre kombinasyonu
                if (log.RequestRate > RATE_THRESHOLD)
                    detected = true;
                if (log.RequestRate > RATE_THRESHOLD * 0.7 && log.AttackDuration > 3)
                    detected = true;
            }
            else if (log.AttackType == AttackType.SqlInjection)
            {
                // Pattern eşleşmesi veya WAF bypass
                if (log.PatternFlag == 1)
                    detected = true;
                if (log.WAFBypassed == 1)
                    detected = true;
                // Birden fazla deneme yapılmışsa şüpheli
                if (log.AttemptCount > 3)
                    detected = true;
            }
            else if (log.AttackType == AttackType.FileAccess)
            {
                // Kısıtlı erişim veya çok sayıda dosya denemesi
                if (log.RestrictedAccess == 1)
                    detected = true;
                if (log.AttemptCount > 5 && log.RestrictedAccess == 1)
                    detected = true;
            }

            return detected;
        }
    }
}