using System.Threading;
using CyberDuel.Models;

namespace CyberDuel.Attacks
{
    public class AttackSimulator
    {
        private Random rng = new Random();

        private string RandomIP()
        {
            return rng.Next(10, 200) + "." + rng.Next(0, 255) + "." +
                   rng.Next(0, 255) + "." + rng.Next(1, 254);
        }

        // ── Brute Force ───────────────────────────────────────────────────────
        public EventLog DoBruteForce(string targetName, Dictionary<string, string> userAccounts)
        {
            // Gerçek saldırılarda kullanılan yaygın şifre listesi
            string[] passwords = {
                "123456", "password", "admin", "root", "qwerty", "12345678",
                "abc123", "letmein", "monkey", "1234567", "dragon", "111111",
                "baseball", "iloveyou", "trustno1", "sunshine", "master",
                "welcome", "shadow", "football", "michael", "ninja",
                "mustang", "password1", "test123"
            };

            string[] usernamesToTry = { "admin", "administrator", "root", "user", "test", "guest" };

            // Hedef sistemdeki gerçek bir hesabı rastgele seç
            var accountList = userAccounts.ToList();
            var realAccount = accountList[rng.Next(accountList.Count)];

            bool success = false;
            bool lockout = false;
            int attempts = 0;
            int lockoutLimit = 10;
            DateTime start = DateTime.Now;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  [*] Initiating Brute Force Attack...");
            Console.WriteLine("  [*] Target: " + targetName);
            Console.ResetColor();
            Thread.Sleep(400);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Loaded " + passwords.Length + " entries from wordlist");
            Console.WriteLine("  [*] Targeting " + usernamesToTry.Length + " username candidates");
            Console.WriteLine();
            Console.ResetColor();
            Thread.Sleep(300);

            foreach (string user in usernamesToTry)
            {
                if (success || lockout) break;

                int failsForUser = 0;

                foreach (string pass in passwords)
                {
                    attempts++;
                    Thread.Sleep(55);

                    bool hit = (user == realAccount.Key && pass == realAccount.Value);

                    if (hit)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  [ATTEMPT " + attempts.ToString().PadLeft(3) + "] " +
                            user.PadRight(15) + " : " + pass.PadRight(15) + " → ACCESS GRANTED ✓");
                        Console.ResetColor();
                        success = true;
                        break;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  [ATTEMPT " + attempts.ToString().PadLeft(3) + "] " +
                            user.PadRight(15) + " : " + pass.PadRight(15) + " → FAILED");
                        Console.ResetColor();
                        failsForUser++;
                    }

                    // Hesap kilitleme mekanizması
                    if (failsForUser >= lockoutLimit)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("\n  [!] ACCOUNT LOCKED: '" + user + "' — Too many failed attempts");
                        Console.WriteLine("  [!] Waiting for cooldown...\n");
                        Console.ResetColor();
                        Thread.Sleep(2000);
                        lockout = true;
                        break;
                    }
                }
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            if (!success)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [-] Brute Force Failed — No valid credentials found.");
                Console.ResetColor();
            }

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.BruteForce;
            log.AttemptCount = attempts;
            log.LockoutTriggered = lockout ? 1 : 0;
            log.AttackDuration = (float)duration;
            log.AttackSuccess = success;
            log.IsMalicious = attempts > 5;
            log.RequestRate = 0;
            log.PortCount = 0;
            log.PatternFlag = 0;
            log.RestrictedAccess = 0;
            log.OpenPortsFound = 0;
            log.WAFBypassed = 0;

            return log;
        }

        // ── Port Scan ─────────────────────────────────────────────────────────
        public EventLog DoPortScan(string targetName, Dictionary<int, (string Service, string Status)> portTable)
        {
            // Tarama tipi seç
            string[] scanTypes = { "Full Scan", "Quick Scan", "Stealth Scan" };
            string scanType = scanTypes[rng.Next(scanTypes.Count())];

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [*] Initiating Port Scan...");
            Console.WriteLine("  [*] Target: " + targetName);
            Console.WriteLine("  [*] Scan Type: " + scanType);
            Console.ResetColor();
            Thread.Sleep(400);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Probing " + portTable.Count + " ports...\n");
            Console.ResetColor();
            Thread.Sleep(300);

            int openCount = 0;
            int closedCount = 0;
            int filteredCount = 0;
            DateTime start = DateTime.Now;

            foreach (var entry in portTable)
            {
                int port = entry.Key;
                string service = entry.Value.Service;
                string status = entry.Value.Status;

                Thread.Sleep(80);

                if (status == "open")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  PORT " + port.ToString().PadLeft(5) + "/tcp   OPEN      " + service);
                    openCount++;
                }
                else if (status == "filtered")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  PORT " + port.ToString().PadLeft(5) + "/tcp   FILTERED  " + service);
                    filteredCount++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  PORT " + port.ToString().PadLeft(5) + "/tcp   CLOSED    " + service);
                    closedCount++;
                }

                Console.ResetColor();
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [SCAN COMPLETE]");
            Console.WriteLine("  Open: " + openCount + "  Filtered: " + filteredCount + "  Closed: " + closedCount);
            Console.ResetColor();

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.PortScan;
            log.PortCount = portTable.Count;
            log.OpenPortsFound = openCount;
            log.AttackDuration = (float)duration;
            log.AttackSuccess = openCount > 0;
            log.IsMalicious = portTable.Count > 15;
            log.AttemptCount = 1;
            log.RequestRate = 0;
            log.PatternFlag = 0;
            log.RestrictedAccess = 0;
            log.LockoutTriggered = 0;
            log.WAFBypassed = 0;

            return log;
        }

        // ── DDoS Flood ────────────────────────────────────────────────────────
        public EventLog DoDDoS(string targetName, int maxCapacity)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  [*] Initiating DDoS Flood Attack...");
            Console.WriteLine("  [*] Target: " + targetName);
            Console.WriteLine("  [*] Server Capacity: " + maxCapacity + " req/s");
            Console.ResetColor();
            Thread.Sleep(400);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Spinning up botnet nodes...");
            Console.ResetColor();
            Thread.Sleep(600);

            int currentRate = 0;
            int step = rng.Next(80, 200);
            string serverStatus = "STABLE";
            bool serverDown = false;
            DateTime start = DateTime.Now;
            int seconds = 0;

            Console.WriteLine();

            while (currentRate < maxCapacity * 1.4)
            {
                seconds++;
                currentRate += step + rng.Next(-30, 50);
                if (currentRate < 0) currentRate = 0;

                Thread.Sleep(300);

                // Sunucu durumu güncelle
                if (currentRate >= maxCapacity)
                {
                    serverStatus = "DOWN";
                    serverDown = true;
                }
                else if (currentRate >= maxCapacity * 0.75)
                    serverStatus = "CRITICAL";
                else if (currentRate >= maxCapacity * 0.5)
                    serverStatus = "DEGRADED";
                else
                    serverStatus = "STABLE";

                // Renk belirle
                if (serverStatus == "DOWN") Console.ForegroundColor = ConsoleColor.DarkRed;
                else if (serverStatus == "CRITICAL") Console.ForegroundColor = ConsoleColor.Red;
                else if (serverStatus == "DEGRADED") Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Green;

                // Progress bar
                int barLength = 30;
                int filled = (int)((float)currentRate / (maxCapacity * 1.4f) * barLength);
                if (filled > barLength) filled = barLength;
                string bar = "[" + new string('#', filled) + new string('-', barLength - filled) + "]";

                Console.WriteLine("  [" + seconds.ToString().PadLeft(2) + "s] " +
                    (currentRate.ToString() + " req/s").PadRight(12) +
                    bar + "  Server: " + serverStatus);
                Console.ResetColor();

                if (serverDown) break;
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            if (serverDown)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [!!!] TARGET IS DOWN — Service unavailable");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  [-] Server survived the flood — DDoS unsuccessful");
                Console.ResetColor();
            }

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.DDoSFlood;
            log.RequestRate = currentRate;
            log.AttackDuration = (float)duration;
            log.AttackSuccess = serverDown;
            log.IsMalicious = currentRate > 500;
            log.AttemptCount = 1;
            log.PortCount = 0;
            log.PatternFlag = 0;
            log.RestrictedAccess = 0;
            log.OpenPortsFound = 0;
            log.LockoutTriggered = 0;
            log.WAFBypassed = 0;

            return log;
        }

        // ── SQL Injection ─────────────────────────────────────────────────────
        public EventLog DoSqlInjection(string targetName, bool hasWAF)
        {
            // Gerçek SQL injection payload listesi
            string[] payloads = {
                "' OR '1'='1",
                "' OR '1'='1' --",
                "'; DROP TABLE users; --",
                "' UNION SELECT * FROM accounts --",
                "admin' --",
                "' OR 1=1 --",
                "1; SELECT * FROM credentials",
                "' OR 'x'='x",
                "') OR ('1'='1",
                "' AND 1=1 --",
                "1' ORDER BY 1 --",
                "' UNION SELECT null, username, password FROM users --"
            };

            string[] targetTables = { "users", "customers", "accounts", "credit_cards", "sessions", "admin_panel" };

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n  [*] Initiating SQL Injection Attack...");
            Console.WriteLine("  [*] Target: " + targetName);
            Console.WriteLine("  [*] WAF Detected: " + (hasWAF ? "YES — Evasion required" : "NO — Direct injection"));
            Console.ResetColor();
            Thread.Sleep(400);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Loaded " + payloads.Length + " injection payloads");
            Console.WriteLine();
            Console.ResetColor();
            Thread.Sleep(300);

            bool success = false;
            bool wafBypassed = false;
            int attempted = 0;
            string successTable = "";
            DateTime start = DateTime.Now;

            foreach (string payload in payloads)
            {
                attempted++;
                Thread.Sleep(120);

                bool blocked = hasWAF && rng.NextDouble() < 0.65; // WAF varsa %65 ihtimalle engeller
                bool injected = !blocked && rng.NextDouble() < 0.55;

                if (blocked)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("  [PAYLOAD " + attempted.ToString().PadLeft(2) + "] " +
                        payload.PadRight(40) + " → WAF: BLOCKED");
                    Console.ResetColor();
                }
                else if (injected)
                {
                    successTable = targetTables[rng.Next(targetTables.Length)];
                    wafBypassed = hasWAF;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [PAYLOAD " + attempted.ToString().PadLeft(2) + "] " +
                        payload.PadRight(40) + " → INJECTED ✓");
                    Console.WriteLine("  [+] Database access obtained → Table: " + successTable);
                    Console.ResetColor();
                    success = true;
                    break;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [PAYLOAD " + attempted.ToString().PadLeft(2) + "] " +
                        payload.PadRight(40) + " → REJECTED");
                    Console.ResetColor();
                }
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            if (!success)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [-] All payloads failed — Injection unsuccessful");
                Console.ResetColor();
            }

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.SqlInjection;
            log.PatternFlag = success ? 1 : 0;
            log.WAFBypassed = wafBypassed ? 1 : 0;
            log.AttemptCount = attempted;
            log.AttackDuration = (float)duration;
            log.AttackSuccess = success;
            log.IsMalicious = attempted > 1;
            log.RequestRate = 0;
            log.PortCount = 0;
            log.RestrictedAccess = 0;
            log.OpenPortsFound = 0;
            log.LockoutTriggered = 0;

            return log;
        }

        // ── Unauthorized File Access ──────────────────────────────────────────
        public EventLog DoFileAccess(string targetName, Dictionary<string, string> fileSystem)
        {
            // Kullanıcı rolü — guest'ten başla, privilege escalation dene
            string[] roles = { "guest", "user", "admin" };
            string currentRole = roles[rng.Next(2)]; // guest veya user olarak başla

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  [*] Initiating Unauthorized File Access...");
            Console.WriteLine("  [*] Target: " + targetName);
            Console.WriteLine("  [*] Current Role: " + currentRole.ToUpper());
            Console.ResetColor();
            Thread.Sleep(400);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Enumerating filesystem paths...\n");
            Console.ResetColor();
            Thread.Sleep(300);

            bool anyRestricted = false;
            bool privilegeEscalated = false;
            int accessCount = 0;
            DateTime start = DateTime.Now;

            // Privilege escalation denemesi
            if (rng.NextDouble() < 0.40)
            {
                Thread.Sleep(200);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("  [*] Attempting privilege escalation via SUID binary...");
                Thread.Sleep(400);

                if (rng.NextDouble() < 0.35)
                {
                    currentRole = "admin";
                    privilegeEscalated = true;
                    Console.WriteLine("  [+] Privilege escalation successful → Role: ADMIN");
                }
                else
                {
                    Console.WriteLine("  [-] Privilege escalation failed — Permission denied");
                }
                Console.ResetColor();
                Console.WriteLine();
            }

            // Dosyalara erişim dene
            foreach (var file in fileSystem)
            {
                string path = file.Key;
                string required = file.Value;
                accessCount++;

                Thread.Sleep(90);

                bool hasAccess = RoleHasAccess(currentRole, required);

                if (hasAccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [ACCESS] " + path.PadRight(35) + " → ALLOWED  (" + currentRole + ")");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [ACCESS] " + path.PadRight(35) + " → DENIED   (requires: " + required + ")");
                    Console.ResetColor();
                    anyRestricted = true;
                }
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now;
            log.SourceIP = RandomIP();
            log.TargetSystem = targetName;
            log.AttackType = AttackType.FileAccess;
            log.RestrictedAccess = anyRestricted ? 1 : 0;
            log.AttemptCount = accessCount;
            log.AttackDuration = (float)duration;
            log.AttackSuccess = privilegeEscalated;
            log.IsMalicious = anyRestricted;
            log.RequestRate = 0;
            log.PortCount = 0;
            log.PatternFlag = 0;
            log.OpenPortsFound = 0;
            log.LockoutTriggered = 0;
            log.WAFBypassed = 0;

            return log;
        }

        // Rol kontrolü — guest < user < admin < root
        private bool RoleHasAccess(string userRole, string required)
        {
            int userLevel = RoleLevel(userRole);
            int requiredLevel = RoleLevel(required);
            return userLevel >= requiredLevel;
        }

        private int RoleLevel(string role)
        {
            if (role == "root") return 4;
            if (role == "admin") return 3;
            if (role == "user") return 2;
            return 1; // guest
        }
    }
}