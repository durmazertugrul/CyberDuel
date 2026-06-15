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

        // ── Port Scan ─────────────────────────────────────────────────────────
        public EventLog DoPortScan(string targetName,
            Dictionary<int, (string Service, string Status)> portTable,
            SessionState state,
            DifficultySettings diff)
        {
            if (state.ServerOffline)
            {
                PrintServerOffline(targetName);
                return MakeOfflineLog(targetName, AttackType.PortScan);
            }

            string[] scanTypes = { "Full Scan", "Quick Scan", "Stealth Scan" };
            string scanType = scanTypes[rng.Next(scanTypes.Length)];
            int idsAlertThreshold = 7;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [*] Initiating Port Scan — " + scanType);
            Console.WriteLine("  [*] Target: " + targetName);
            Console.ResetColor();
            Thread.Sleep(400);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Probing " + portTable.Count + " ports...\n");
            Console.ResetColor();
            Thread.Sleep(300);

            int openCount = 0;
            int closedCount = 0;
            int filteredCount = 0;
            int scanned = 0;
            bool idsTriggered = false;
            DateTime start = DateTime.Now;

            // Portları bir listeye çevir, sonra tekrar sözlüğe eriş
            var portList = portTable.ToList();

            foreach (var entry in portList)
            {
                int port = entry.Key;
                string service = entry.Value.Service;
                string status = entry.Value.Status;
                scanned++;

                Thread.Sleep(80);

                // IDS aktifse belirli noktada bazı portları filtrele
                if (diff.IDSActive && scanned == idsAlertThreshold && !idsTriggered)
                {
                    idsTriggered = true;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\n  [IDS ALERT] Suspicious scanning activity detected — firewall adapting...");
                    Console.ResetColor();
                    Thread.Sleep(800);
                    Console.WriteLine();
                }

                // IDS tetiklendiyse açık portların bir kısmı filtered'e dönüşür
                if (idsTriggered && status == "open" && rng.NextDouble() < 0.35)
                    status = "filtered";

                if (status == "open")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  PORT " + port.ToString().PadLeft(5) + "/tcp   OPEN      " + service);
                    openCount++;
                    if (!state.DiscoveredOpenPorts.Contains(port))
                        state.DiscoveredOpenPorts.Add(port);
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

            if (openCount > 0)
            {
                Console.WriteLine("\n  [+] Open ports saved to session state — use for targeted attacks");
            }
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

        // ── Brute Force ───────────────────────────────────────────────────────
        public EventLog DoBruteForce(string targetName,
            Dictionary<string, string> userAccounts,
            SessionState state,
            DifficultySettings diff)
        {
            if (state.ServerOffline)
            {
                PrintServerOffline(targetName);
                return MakeOfflineLog(targetName, AttackType.BruteForce);
            }

            string[] passwords = {
                "123456", "password", "admin", "root", "qwerty", "12345678",
                "abc123", "letmein", "monkey", "1234567", "dragon", "111111",
                "baseball", "iloveyou", "trustno1", "sunshine", "master",
                "welcome", "shadow", "football", "michael", "ninja",
                "mustang", "password1", "test123"
            };

            string[] usernamesToTry = { "admin", "administrator", "root", "user", "test", "guest" };

            var accountList = userAccounts.ToList();
            var realAccount = accountList[rng.Next(accountList.Count)];

            // Zorluğa göre şifre wordlist'te olmayabilir
            bool passwordInWordlist = rng.NextDouble() >= diff.PasswordMissingChance;

            bool success = false;
            bool lockout = false;
            int attempts = 0;
            int failsForCurrentUser = 0;
            DateTime start = DateTime.Now;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  [*] Initiating Brute Force Attack...");
            Console.WriteLine("  [*] Target: " + targetName);
            Console.WriteLine("  [*] Difficulty: " + diff.Name + " — Lockout after " + diff.LockoutThreshold + " fails");
            Console.ResetColor();
            Thread.Sleep(400);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Loaded " + passwords.Length + " entries from wordlist");
            Console.WriteLine("  [*] Password in wordlist: " + (passwordInWordlist ? "YES" : "NO (attack will fail)"));
            Console.WriteLine();
            Console.ResetColor();
            Thread.Sleep(300);

            // Önceki port scan'de SSH açık bulduysa bunu belirt
            if (state.DiscoveredOpenPorts.Contains(22))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  [+] SSH (port 22) discovered in previous scan — targeting SSH login");
                Console.ResetColor();
                Thread.Sleep(300);
            }

            foreach (string user in usernamesToTry)
            {
                if (success || lockout) break;
                failsForCurrentUser = 0;

                foreach (string pass in passwords)
                {
                    attempts++;
                    Thread.Sleep(55);

                    bool hit = passwordInWordlist &&
                               (user == realAccount.Key && pass == realAccount.Value);

                    if (hit)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  [ATTEMPT " + attempts.ToString().PadLeft(3) + "] " +
                            user.PadRight(15) + " : " + pass.PadRight(15) + " → ACCESS GRANTED ✓");
                        Console.ResetColor();
                        success = true;
                        state.AdminAccessGained = true;
                        state.CompromisedUsername = user;
                        state.CompromisedPassword = pass;
                        break;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  [ATTEMPT " + attempts.ToString().PadLeft(3) + "] " +
                            user.PadRight(15) + " : " + pass.PadRight(15) + " → FAILED");
                        Console.ResetColor();
                        failsForCurrentUser++;
                    }

                    // IDS aktifse yarı noktada bağlantı yavaşlatma
                    if (diff.IDSActive && attempts == 5)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("\n  [IDS] Repeated login failures detected — connection throttled (2s delay)");
                        Console.ResetColor();
                        Thread.Sleep(2000);
                    }

                    // Lockout
                    if (failsForCurrentUser >= diff.LockoutThreshold)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("\n  [!] ACCOUNT LOCKED: '" + user + "' — Lockout threshold reached");
                        Console.WriteLine("  [!] Waiting for cooldown...\n");
                        Console.ResetColor();
                        Thread.Sleep(2000);
                        lockout = true;
                        break;
                    }
                }
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  [+] Credentials obtained: " +
                    state.CompromisedUsername + " / " + state.CompromisedPassword);
                Console.WriteLine("  [+] Admin access gained — file access privileges elevated");
                Console.ResetColor();
            }
            else if (!passwordInWordlist)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("\n  [-] Password not found in wordlist — wordlist expansion required");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [-] Brute Force Failed — No valid credentials found");
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

        // ── DDoS Flood ────────────────────────────────────────────────────────
        public EventLog DoDDoS(string targetName,
            int baseCapacity,
            SessionState state,
            DifficultySettings diff)
        {
            if (state.ServerOffline)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [*] " + targetName + " is already offline.");
                Console.ResetColor();
                return MakeOfflineLog(targetName, AttackType.DDoSFlood);
            }

            int maxCapacity = (int)(baseCapacity * diff.CapacityMultiplier);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  [*] Initiating DDoS Flood Attack...");
            Console.WriteLine("  [*] Target: " + targetName);
            Console.WriteLine("  [*] Server Capacity: " + maxCapacity + " req/s (" + diff.Name + ")");
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
            bool idsRateLimiting = false;
            DateTime start = DateTime.Now;
            int seconds = 0;
            int barLength = 30;

            Console.WriteLine();

            while (currentRate < maxCapacity * 1.4)
            {
                seconds++;

                // IDS aktifse DEGRADED noktasında rate limiting başlatır
                if (diff.IDSActive && currentRate >= maxCapacity * 0.5 && !idsRateLimiting)
                {
                    idsRateLimiting = true;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\n  [IDS] Anomalous traffic detected — rate limiting activated\n");
                    Console.ResetColor();
                    Thread.Sleep(500);
                    step = (int)(step * 0.55); // rate artışı yavaşlıyor
                }

                currentRate += step + rng.Next(-30, 50);
                if (currentRate < 0) currentRate = 0;

                Thread.Sleep(300);

                if (currentRate >= maxCapacity)
                {
                    serverStatus = "DOWN";
                    serverDown = true;
                }
                else if (currentRate >= maxCapacity * 0.75)
                    serverStatus = "CRITICAL";
                else if (currentRate >= maxCapacity * 0.50)
                    serverStatus = "DEGRADED";
                else
                    serverStatus = "STABLE";

                if (serverStatus == "DOWN") Console.ForegroundColor = ConsoleColor.DarkRed;
                else if (serverStatus == "CRITICAL") Console.ForegroundColor = ConsoleColor.Red;
                else if (serverStatus == "DEGRADED") Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Green;

                int filled = (int)((float)currentRate / (maxCapacity * 1.4f) * barLength);
                if (filled > barLength) filled = barLength;
                string bar = "[" + new string('#', filled) + new string('-', barLength - filled) + "]";
                string limiter = idsRateLimiting ? " [IDS:LIMIT]" : "";

                Console.WriteLine("  [" + seconds.ToString().PadLeft(2) + "s] " +
                    (currentRate.ToString() + " req/s").PadRight(12) +
                    bar + "  " + serverStatus + limiter);
                Console.ResetColor();

                if (serverDown) break;
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            if (serverDown)
            {
                state.ServerOffline = true;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [!!!] " + targetName + " IS DOWN — Service unavailable");
                Console.WriteLine("  [!!!] Server will remain offline for this session");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  [-] Server survived the flood" +
                    (idsRateLimiting ? " — IDS rate limiting prevented takedown" : ""));
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
        public EventLog DoSqlInjection(string targetName,
            bool hasWAF,
            SessionState state,
            DifficultySettings diff)
        {
            if (state.ServerOffline)
            {
                PrintServerOffline(targetName);
                return MakeOfflineLog(targetName, AttackType.SqlInjection);
            }

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

            // Önceki port scan'de MSSQL (1433) bulunduysa başarı bonusu
            bool sqlPortKnown = state.DiscoveredOpenPorts.Contains(1433) ||
                                state.DiscoveredOpenPorts.Contains(3306);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n  [*] Initiating SQL Injection Attack...");
            Console.WriteLine("  [*] Target: " + targetName);
            Console.WriteLine("  [*] WAF Detected: " + (hasWAF ? "YES — Evasion required" : "NO — Direct injection"));
            if (sqlPortKnown)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  [+] DB port discovered in previous scan — injection targeting active DB");
            }
            Console.ResetColor();
            Thread.Sleep(400);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Loaded " + payloads.Length + " injection payloads\n");
            Console.ResetColor();
            Thread.Sleep(300);

            bool success = false;
            bool wafBypassed = false;
            int attempted = 0;
            string successTable = "";
            DateTime start = DateTime.Now;

            // Port bilgisi varsa injection başarı oranı artar
            double successBonus = sqlPortKnown ? 0.15 : 0.0;

            foreach (string payload in payloads)
            {
                attempted++;
                Thread.Sleep(120);

                bool blocked = hasWAF && rng.NextDouble() < diff.WAFBlockRate;
                bool injected = !blocked && rng.NextDouble() < (diff.InjectionSuccessRate + successBonus);

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
                    Console.ResetColor();
                    success = true;
                    state.DatabaseBreached = true;
                    state.BreachedTable = successTable;
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

            if (success)
            {
                // Sahte çekilen veri göster
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  [+] Database access obtained → Table: " + successTable);
                Console.WriteLine("\n  [EXTRACTED DATA — " + successTable.ToUpper() + "]");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  +----+------------------+-------------------------+----------------+");
                Console.WriteLine("  | ID | Name             | Email                   | Sensitive      |");
                Console.WriteLine("  +----+------------------+-------------------------+----------------+");
                Console.WriteLine("  |  1 | John Smith       | jsmith@example.com      | ****-****-1234 |");
                Console.WriteLine("  |  2 | Emma Wilson      | ewilson@corp.com        | ****-****-5678 |");
                Console.WriteLine("  |  3 | Michael Brown    | mbrown@mail.com         | ****-****-9012 |");
                Console.WriteLine("  | .. | ...              | ...                     | ...            |");
                Console.WriteLine("  +----+------------------+-------------------------+----------------+");
                Console.WriteLine("  1,247 records extracted");
                Console.ResetColor();
            }
            else
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
        public EventLog DoFileAccess(string targetName,
            Dictionary<string, string> fileSystem,
            SessionState state,
            DifficultySettings diff)
        {
            if (state.ServerOffline)
            {
                PrintServerOffline(targetName);
                return MakeOfflineLog(targetName, AttackType.FileAccess);
            }

            // Önceki brute force başarısıysa elevated role ile başla
            string currentRole = "guest";
            bool alreadyElevated = false;

            if (state.AdminAccessGained)
            {
                currentRole = "admin";
                alreadyElevated = true;
            }
            else if (rng.NextDouble() > 0.5)
            {
                currentRole = "user";
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  [*] Initiating Unauthorized File Access...");
            Console.WriteLine("  [*] Target: " + targetName);

            if (alreadyElevated)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [+] Admin credentials from previous brute force — starting as ADMIN");
            }
            else
            {
                Console.WriteLine("  [*] Current Role: " + currentRole.ToUpper());
            }
            Console.ResetColor();
            Thread.Sleep(400);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Enumerating filesystem paths...\n");
            Console.ResetColor();
            Thread.Sleep(300);

            bool anyRestricted = false;
            bool privilegeEscalated = false;

            // Henüz elevated değilse privilege escalation dene
            if (!alreadyElevated)
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
                    Console.WriteLine("  [-] Privilege escalation failed — continuing as " + currentRole.ToUpper());
                }
                Console.ResetColor();
                Console.WriteLine();
            }

            int accessCount = 0;
            DateTime start = DateTime.Now;

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

                    // Admin ise hassas dosyalarda içerik göster
                    if (currentRole == "admin" && path.Contains("config"))
                    {
                        Thread.Sleep(100);
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("           > <db_host>192.168.10.100</db_host>");
                        Console.WriteLine("           > <db_user>admin</db_user>");
                        Console.WriteLine("           > <db_pass>F!n@nce_DB_2024</db_pass>");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [ACCESS] " + path.PadRight(35) + " → DENIED   (requires: " + required + ")");
                    Console.ResetColor();
                    anyRestricted = true;

                    // IDS aktifse hassas path erişiminde uyarı
                    if (diff.IDSActive && (path.Contains("shadow") || path.Contains("credentials")))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("  [IDS ALERT] Attempt to access critical system path — incident logged");
                        Console.ResetColor();
                        Thread.Sleep(400);
                    }
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
            log.AttackSuccess = privilegeEscalated || alreadyElevated;
            log.IsMalicious = anyRestricted;
            log.RequestRate = 0;
            log.PortCount = 0;
            log.PatternFlag = 0;
            log.OpenPortsFound = 0;
            log.LockoutTriggered = 0;
            log.WAFBypassed = 0;

            return log;
        }

        // ── Yardımcılar ───────────────────────────────────────────────────────
        private bool RoleHasAccess(string userRole, string required)
        {
            return RoleLevel(userRole) >= RoleLevel(required);
        }

        private int RoleLevel(string role)
        {
            if (role == "root") return 4;
            if (role == "admin") return 3;
            if (role == "user") return 2;
            return 1;
        }

        private void PrintServerOffline(string targetName)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\n  [!!!] " + targetName + " is OFFLINE — attack cannot be executed");
            Console.ResetColor();
            Thread.Sleep(800);
        }

        private EventLog MakeOfflineLog(string targetName, AttackType type)
        {
            return new EventLog
            {
                Timestamp = DateTime.Now,
                SourceIP = RandomIP(),
                TargetSystem = targetName,
                AttackType = type,
                AttackSuccess = false,
                IsMalicious = false
            };
        }
    }
}