using CyberDuel.Models;

namespace CyberDuel.Detection
{
    public class RuleEngine
    {
        private const int PORT_THRESHOLD = 15;
        private const int ATTEMPT_THRESHOLD = 5;
        private const double RATE_THRESHOLD = 500.0;

        public bool Check(EventLog log)
        {
            bool detected = false;
            if (log.AttackType == AttackType.PortScan)
            {
                if (log.PortCount > PORT_THRESHOLD) detected = true;
                if (log.OpenPortsFound >= 3) detected = true;
            }
            else if (log.AttackType == AttackType.BruteForce)
            {
                if (log.AttemptCount > ATTEMPT_THRESHOLD) detected = true;
                if (log.LockoutTriggered == 1) detected = true;
            }
            else if (log.AttackType == AttackType.DDoSFlood)
            {
                if (log.RequestRate > RATE_THRESHOLD) detected = true;
                if (log.RequestRate > RATE_THRESHOLD * 0.7 && log.AttackDuration > 3) detected = true;
            }
            else if (log.AttackType == AttackType.SqlInjection)
            {
                if (log.PatternFlag == 1) detected = true;
                if (log.WAFBypassed == 1) detected = true;
                if (log.AttemptCount > 3) detected = true;
            }
            else if (log.AttackType == AttackType.FileAccess)
            {
                if (log.RestrictedAccess == 1) detected = true;
                if (log.AttemptCount > 5 && log.RestrictedAccess == 1) detected = true;
            }
            return detected;
        }
    }
}