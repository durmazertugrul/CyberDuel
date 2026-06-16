namespace CyberDuel.Detection
{
    public class IPTracker
    {
        private Dictionary<string, int> attacksByIP = new Dictionary<string, int>();
        private Dictionary<string, int> detectionsByIP = new Dictionary<string, int>();
        private HashSet<string> bannedIPs = new HashSet<string>();
        private List<string> knownIPs = new List<string>();
        private const int BanThreshold = 3;

        public void Record(string ip)
        {
            if (!attacksByIP.ContainsKey(ip)) attacksByIP[ip] = 0;
            attacksByIP[ip]++;
            if (!knownIPs.Contains(ip)) knownIPs.Add(ip);
        }

        public void RecordDetection(string ip)
        {
            if (!detectionsByIP.ContainsKey(ip)) detectionsByIP[ip] = 0;
            detectionsByIP[ip]++;
            if (detectionsByIP[ip] >= BanThreshold && !bannedIPs.Contains(ip))
            {
                bannedIPs.Add(ip);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [IDS] IP BANNED: " + ip + " — " + BanThreshold + " detections reached");
                Console.ResetColor();
            }
        }

        public bool IsBanned(string ip) => bannedIPs.Contains(ip);
        public bool IsRepeatOffender(string ip) => attacksByIP.ContainsKey(ip) && attacksByIP[ip] >= 2;
        public int GetAttackCount(string ip) => attacksByIP.ContainsKey(ip) ? attacksByIP[ip] : 0;
        public int BannedCount => bannedIPs.Count;

        public string GetOrNewIP(Random rng, double reuseChance)
        {
            List<string> available = knownIPs.Where(ip => !bannedIPs.Contains(ip)).ToList();
            if (available.Count > 0 && rng.NextDouble() < reuseChance)
                return available[rng.Next(available.Count)];
            string newIP; int tries = 0;
            do
            {
                newIP = rng.Next(10, 200) + "." + rng.Next(0, 255) + "." + rng.Next(0, 255) + "." + rng.Next(1, 254);
                tries++;
            } while (bannedIPs.Contains(newIP) && tries < 20);
            return newIP;
        }

        public void Reset()
        {
            attacksByIP.Clear(); detectionsByIP.Clear(); bannedIPs.Clear(); knownIPs.Clear();
        }
    }
}