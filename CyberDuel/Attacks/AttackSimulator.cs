using System.Threading;
using CyberDuel.Models;
using CyberDuel.Detection;

namespace CyberDuel.Attacks
{
    public class AttackSimulator
    {
        private Random rng = new Random();

        // ── Port Scan ─────────────────────────────────────────────────────────
        public EventLog DoPortScan(string targetName,
            Dictionary<int, (string Service, string Status)> portTable,
            SessionState state, DifficultySettings diff,
            IPTracker ipTracker, bool stealth)
        {
            if (state.ServerOffline) return OfflineLog(targetName, AttackType.PortScan, ipTracker);

            string sourceIP = ipTracker.GetOrNewIP(rng, stealth ? 0.15 : 0.25);
            ipTracker.Record(sourceIP);
            bool repeatOffender = ipTracker.IsRepeatOffender(sourceIP);
            int delay = stealth ? 160 : 80;
            int idsThreshold = stealth ? 12 : 7;
            bool idsTriggered = false;
            int openCount = 0, closedCount = 0, filteredCount = 0, scanned = 0;
            DateTime start = DateTime.Now;

            var portsToScan = stealth
                ? portTable.Where(p => rng.NextDouble() < 0.6).ToList()
                : portTable.ToList();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [*] Initiating Port Scan — " + (stealth ? "Stealth" : "Full Scan"));
            Console.WriteLine("  [*] Source IP: " + sourceIP + (repeatOffender ? "  [IDS: KNOWN IP]" : ""));
            Console.WriteLine("  [*] Probing " + portsToScan.Count + " ports...\n");
            Console.ResetColor();
            Thread.Sleep(400);

            foreach (var entry in portsToScan)
            {
                scanned++;
                Thread.Sleep(delay);
                string status = entry.Value.Status;

                if (diff.IDSActive && scanned == idsThreshold && !idsTriggered)
                {
                    idsTriggered = true;
                    double penalty = repeatOffender ? 0.55 : 0.35;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\n  [IDS] " + (repeatOffender ? "KNOWN IP — " : "") + "Port scan detected — firewall adapting...\n");
                    Console.ResetColor();
                    Thread.Sleep(stealth ? 400 : 800);
                    if (status == "open" && rng.NextDouble() < penalty) status = "filtered";
                }

                if (status == "open") { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("  PORT " + entry.Key.ToString().PadLeft(5) + "/tcp   OPEN      " + entry.Value.Service); openCount++; if (!state.DiscoveredOpenPorts.Contains(entry.Key)) state.DiscoveredOpenPorts.Add(entry.Key); }
                else if (status == "filtered") { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("  PORT " + entry.Key.ToString().PadLeft(5) + "/tcp   FILTERED  " + entry.Value.Service); filteredCount++; }
                else { Console.ForegroundColor = ConsoleColor.DarkGray; Console.WriteLine("  PORT " + entry.Key.ToString().PadLeft(5) + "/tcp   CLOSED    " + entry.Value.Service); closedCount++; }
                Console.ResetColor();
            }

            double duration = (DateTime.Now - start).TotalSeconds;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  [SCAN COMPLETE] Open: " + openCount + "  Filtered: " + filteredCount + "  Closed: " + closedCount);
            if (openCount > 0) Console.WriteLine("  [+] Open ports stored for subsequent attacks");
            Console.ResetColor();

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now; log.SourceIP = sourceIP; log.TargetSystem = targetName;
            log.AttackType = AttackType.PortScan; log.PortCount = portsToScan.Count;
            log.OpenPortsFound = openCount; log.AttackDuration = (float)duration;
            log.AttackSuccess = openCount > 0; log.IsMalicious = portsToScan.Count > 15 || openCount >= 3;
            log.AttemptCount = 1; log.RequestRate = 0; log.PatternFlag = 0;
            log.RestrictedAccess = 0; log.LockoutTriggered = 0; log.WAFBypassed = 0;
            return log;
        }

        // ── Brute Force ───────────────────────────────────────────────────────
        public EventLog DoBruteForce(string targetName,
            Dictionary<string, string> userAccounts,
            SessionState state, DifficultySettings diff,
            IPTracker ipTracker, bool stealth)
        {
            if (state.ServerOffline) return OfflineLog(targetName, AttackType.BruteForce, ipTracker);

            string sourceIP = ipTracker.GetOrNewIP(rng, stealth ? 0.15 : 0.25);
            ipTracker.Record(sourceIP);
            bool repeatOffender = ipTracker.IsRepeatOffender(sourceIP);

            string[] passwords = {
                "123456","password","admin","root","qwerty","12345678","abc123","letmein",
                "monkey","1234567","dragon","111111","baseball","iloveyou","trustno1",
                "sunshine","master","welcome","shadow","football","michael","ninja",
                "mustang","password1","test123"
            };

            // Port bazlı gerçek chaining
            string[] usernamesToTry;
            string serviceTarget = "Web Login";
            if (state.DiscoveredOpenPorts.Contains(22))
            { usernamesToTry = new[] { "root", "ubuntu", "ec2-user", "admin", "git", "deploy", "user" }; serviceTarget = "SSH (port 22)"; }
            else if (state.DiscoveredOpenPorts.Contains(389) || state.DiscoveredOpenPorts.Contains(636))
            { usernamesToTry = new[] { "ldapuser", "svcaccount", "binduser", "admin", "directory", "ldap" }; serviceTarget = "LDAP (port 389/636)"; }
            else
            { usernamesToTry = new[] { "admin", "administrator", "root", "user", "test", "guest" }; }

            bool passwordInWordlist = rng.NextDouble() >= diff.PasswordMissingChance;
            var accountList = userAccounts.ToList();
            var realAccount = accountList[rng.Next(accountList.Count)];
            int maxAttemptsPerUser = stealth ? 8 : passwords.Length;
            int lockoutLimit = stealth ? diff.LockoutThreshold + 3 : diff.LockoutThreshold;
            bool success = false, lockout = false;
            int attempts = 0, failsForUser = 0;
            int idsThrottle = repeatOffender ? 3 : 5;
            DateTime start = DateTime.Now;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  [*] Initiating Brute Force — " + serviceTarget);
            Console.WriteLine("  [*] Source IP: " + sourceIP + (repeatOffender ? "  [IDS: FLAGGED IP]" : ""));
            Console.WriteLine("  [*] Mode: " + (stealth ? "Stealth" : "Normal") + " | Lockout after: " + lockoutLimit + " fails");
            Console.ResetColor();
            Thread.Sleep(400);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Password in wordlist: " + (passwordInWordlist ? "YES" : "NO") + "\n");
            Console.ResetColor();
            Thread.Sleep(300);

            foreach (string user in usernamesToTry)
            {
                if (success || lockout) break;
                failsForUser = 0;
                int attemptsThisUser = 0;

                foreach (string pass in passwords)
                {
                    if (attemptsThisUser >= maxAttemptsPerUser) break;
                    attempts++; attemptsThisUser++;
                    Thread.Sleep(stealth ? 120 : 55);

                    bool hit = passwordInWordlist && user == realAccount.Key && pass == realAccount.Value;

                    if (hit)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  [ATTEMPT " + attempts.ToString().PadLeft(3) + "] " + user.PadRight(15) + " : " + pass.PadRight(15) + " → ACCESS GRANTED ✓");
                        Console.ResetColor();
                        success = true; state.AdminAccessGained = true;
                        state.CompromisedUsername = user; state.CompromisedPassword = pass;
                        break;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  [ATTEMPT " + attempts.ToString().PadLeft(3) + "] " + user.PadRight(15) + " : " + pass.PadRight(15) + " → FAILED");
                        Console.ResetColor();
                        failsForUser++;
                    }

                    if (diff.IDSActive && attempts == idsThrottle)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("\n  [IDS] " + (repeatOffender ? "KNOWN IP — " : "") + "Login failures — throttle: 3s delay");
                        Console.ResetColor();
                        Thread.Sleep(3000);
                    }

                    if (failsForUser >= lockoutLimit)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("\n  [!] ACCOUNT LOCKED: '" + user + "' — waiting...\n");
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
                Console.WriteLine("\n  [+] Credentials: " + state.CompromisedUsername + " / " + state.CompromisedPassword);
                Console.WriteLine("  [+] File access will start with elevated privileges");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [-] " + (!passwordInWordlist ? "Password not in wordlist." : "No credentials found."));
                Console.ResetColor();
            }

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now; log.SourceIP = sourceIP; log.TargetSystem = targetName;
            log.AttackType = AttackType.BruteForce; log.AttemptCount = attempts;
            log.LockoutTriggered = lockout ? 1 : 0; log.AttackDuration = (float)duration;
            log.AttackSuccess = success; log.IsMalicious = attempts > 5;
            log.RequestRate = 0; log.PortCount = 0; log.PatternFlag = 0;
            log.RestrictedAccess = 0; log.OpenPortsFound = 0; log.WAFBypassed = 0;
            return log;
        }

        // ── DDoS Flood ────────────────────────────────────────────────────────
        public EventLog DoDDoS(string targetName,
            int baseCapacity, SessionState state,
            DifficultySettings diff, IPTracker ipTracker, bool stealth)
        {
            if (state.ServerOffline)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [*] " + targetName + " is already offline.");
                Console.ResetColor();
                return OfflineLog(targetName, AttackType.DDoSFlood, ipTracker);
            }

            string sourceIP = ipTracker.GetOrNewIP(rng, stealth ? 0.10 : 0.30);
            ipTracker.Record(sourceIP);
            bool repeatOffender = ipTracker.IsRepeatOffender(sourceIP);
            int maxCapacity = (int)(baseCapacity * diff.CapacityMultiplier);
            int baseStep = stealth ? rng.Next(30, 80) : rng.Next(80, 200);
            int currentRate = 0, seconds = 0, step = baseStep;
            bool serverDown = false, idsRateLimiting = false;
            double idsThreshold = repeatOffender ? 0.35 : 0.50;
            DateTime start = DateTime.Now;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  [*] Initiating DDoS Flood — " + (stealth ? "Low & Slow" : "Full Flood"));
            Console.WriteLine("  [*] Source IP: " + sourceIP + (repeatOffender ? "  [IDS: KNOWN IP]" : ""));
            Console.WriteLine("  [*] Server Capacity: " + maxCapacity + " req/s");
            Console.ResetColor();
            Thread.Sleep(600);
            Console.WriteLine();

            while (currentRate < maxCapacity * 1.4)
            {
                seconds++;
                if (diff.IDSActive && currentRate >= maxCapacity * idsThreshold && !idsRateLimiting)
                {
                    idsRateLimiting = true;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\n  [IDS] " + (repeatOffender ? "KNOWN ATTACKER — " : "") + "Anomalous traffic — rate limiting activated\n");
                    Console.ResetColor();
                    Thread.Sleep(500);
                    step = (int)(step * (stealth ? 0.70 : 0.50));
                }

                currentRate += step + rng.Next(-20, 40);
                if (currentRate < 0) currentRate = 0;
                Thread.Sleep(stealth ? 500 : 300);

                string serverStatus = currentRate >= maxCapacity ? "DOWN" : currentRate >= maxCapacity * 0.75 ? "CRITICAL" : currentRate >= maxCapacity * 0.50 ? "DEGRADED" : "STABLE";
                if (serverStatus == "DOWN") Console.ForegroundColor = ConsoleColor.DarkRed;
                else if (serverStatus == "CRITICAL") Console.ForegroundColor = ConsoleColor.Red;
                else if (serverStatus == "DEGRADED") Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Green;

                int filled = Math.Min(30, (int)((float)currentRate / (maxCapacity * 1.4f) * 30));
                string bar = "[" + new string('#', filled) + new string('-', 30 - filled) + "]";
                Console.WriteLine("  [" + seconds.ToString().PadLeft(2) + "s] " + (currentRate + " req/s").PadRight(12) + bar + "  " + serverStatus + (idsRateLimiting ? " [RATE-LIMITED]" : ""));
                Console.ResetColor();

                if (serverStatus == "DOWN") { serverDown = true; break; }
            }

            double duration = (DateTime.Now - start).TotalSeconds;
            if (serverDown)
            {
                state.ServerOffline = true;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n  [!!!] " + targetName + " IS DOWN — all further attacks blocked");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n  [-] Server survived" + (idsRateLimiting ? " — IDS rate limiting held" : ""));
                Console.ResetColor();
            }

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now; log.SourceIP = sourceIP; log.TargetSystem = targetName;
            log.AttackType = AttackType.DDoSFlood; log.RequestRate = currentRate;
            log.AttackDuration = (float)duration; log.AttackSuccess = serverDown;
            log.IsMalicious = currentRate > 500; log.AttemptCount = 1; log.PortCount = 0;
            log.PatternFlag = 0; log.RestrictedAccess = 0; log.OpenPortsFound = 0;
            log.LockoutTriggered = 0; log.WAFBypassed = 0;
            return log;
        }

        // ── SQL Injection ─────────────────────────────────────────────────────
        public EventLog DoSqlInjection(string targetName,
            bool hasWAF, SessionState state,
            DifficultySettings diff, IPTracker ipTracker, bool stealth)
        {
            if (state.ServerOffline) return OfflineLog(targetName, AttackType.SqlInjection, ipTracker);

            string sourceIP = ipTracker.GetOrNewIP(rng, stealth ? 0.10 : 0.25);
            ipTracker.Record(sourceIP);
            bool repeatOffender = ipTracker.IsRepeatOffender(sourceIP);

            string[] payloads = {
                "' OR '1'='1", "' OR '1'='1' --", "'; DROP TABLE users; --",
                "' UNION SELECT * FROM accounts --", "admin' --", "' OR 1=1 --",
                "1; SELECT * FROM credentials", "' OR 'x'='x", "') OR ('1'='1",
                "' AND 1=1 --", "1' ORDER BY 1 --",
                "' UNION SELECT null, username, password FROM users --"
            };

            bool dbPortKnown = state.DiscoveredOpenPorts.Contains(1433) || state.DiscoveredOpenPorts.Contains(3306);
            double successBonus = dbPortKnown ? 0.20 : 0.0;

            // Stealth modda daha az payload — başarı şansı orantılı olarak düşer
            int maxPayloads = stealth ? 6 : payloads.Length;
            double scaledRate = (diff.InjectionSuccessRate + successBonus) * ((double)maxPayloads / payloads.Length);
            double wafRate = stealth ? diff.WAFBlockRate * 0.75 : diff.WAFBlockRate;

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n  [*] Initiating SQL Injection — " + (stealth ? "Encoded Payloads" : "Direct Injection"));
            Console.WriteLine("  [*] Source IP: " + sourceIP + (repeatOffender ? "  [IDS: KNOWN IP]" : ""));
            Console.WriteLine("  [*] WAF: " + (hasWAF ? "YES" : "NO") + (dbPortKnown ? " | DB port known — targeting active service" : ""));
            Console.WriteLine("  [*] Payloads: " + maxPayloads + "/" + payloads.Length + (stealth ? "  (stealth — success rate: " + (scaledRate * 100).ToString("F0") + "%)" : ""));
            Console.ResetColor();
            Thread.Sleep(400);

            bool success = false, wafBypassed = false;
            int attempted = 0;
            string successTable = "";
            DateTime start = DateTime.Now;
            string[] tables = GetTargetTables(targetName);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [*] Loaded " + maxPayloads + " payloads\n");
            Console.ResetColor();

            for (int i = 0; i < Math.Min(maxPayloads, payloads.Length); i++)
            {
                attempted++;
                Thread.Sleep(stealth ? 200 : 120);
                bool blocked = hasWAF && rng.NextDouble() < wafRate;
                bool injected = !blocked && rng.NextDouble() < scaledRate;

                if (blocked) { Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine("  [PAYLOAD " + attempted.ToString().PadLeft(2) + "] " + payloads[i].PadRight(40) + " → WAF: BLOCKED"); }
                else if (injected)
                {
                    successTable = tables[rng.Next(tables.Length)];
                    wafBypassed = hasWAF;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [PAYLOAD " + attempted.ToString().PadLeft(2) + "] " + payloads[i].PadRight(40) + " → INJECTED ✓");
                    success = true; state.DatabaseBreached = true; state.BreachedTable = successTable;
                }
                else { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("  [PAYLOAD " + attempted.ToString().PadLeft(2) + "] " + payloads[i].PadRight(40) + " → REJECTED"); }
                Console.ResetColor();
                if (success) break;
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            if (success) PrintExtractedData(targetName, successTable);
            else { Console.ForegroundColor = ConsoleColor.DarkRed; Console.WriteLine("\n  [-] All payloads failed." + (stealth ? " (stealth mode reduced success probability)" : "")); Console.ResetColor(); }

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now; log.SourceIP = sourceIP; log.TargetSystem = targetName;
            log.AttackType = AttackType.SqlInjection; log.PatternFlag = success ? 1 : 0;
            log.WAFBypassed = wafBypassed ? 1 : 0; log.AttemptCount = attempted;
            log.AttackDuration = (float)duration; log.AttackSuccess = success;
            log.IsMalicious = attempted > 1; log.RequestRate = 0; log.PortCount = 0;
            log.RestrictedAccess = 0; log.OpenPortsFound = 0; log.LockoutTriggered = 0;
            return log;
        }

        // ── File Access ───────────────────────────────────────────────────────
        public EventLog DoFileAccess(string targetName,
            Dictionary<string, string> fileSystem,
            SessionState state, DifficultySettings diff,
            IPTracker ipTracker, bool stealth)
        {
            if (state.ServerOffline) return OfflineLog(targetName, AttackType.FileAccess, ipTracker);

            string sourceIP = ipTracker.GetOrNewIP(rng, stealth ? 0.10 : 0.20);
            ipTracker.Record(sourceIP);
            bool repeatOffender = ipTracker.IsRepeatOffender(sourceIP);

            string currentRole = state.AdminAccessGained ? "admin" : (rng.NextDouble() > 0.5 ? "user" : "guest");
            bool alreadyElevated = state.AdminAccessGained;
            bool privilegeEscalated = false;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  [*] Initiating File Access — " + (stealth ? "Quiet Enumeration" : "Full Enumeration"));
            Console.WriteLine("  [*] Source IP: " + sourceIP + (repeatOffender ? "  [IDS: KNOWN IP]" : ""));
            if (alreadyElevated) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("  [+] Admin credentials active — starting as ADMIN"); }
            else Console.WriteLine("  [*] Current Role: " + currentRole.ToUpper());
            Console.ResetColor();
            Thread.Sleep(400);

            if (!alreadyElevated && !stealth)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("\n  [*] Attempting privilege escalation via SUID binary...");
                Thread.Sleep(500);
                if (rng.NextDouble() < 0.35) { currentRole = "admin"; privilegeEscalated = true; Console.WriteLine("  [+] Privilege escalation successful → Role: ADMIN"); }
                else Console.WriteLine("  [-] Privilege escalation failed");
                Console.ResetColor();
                Console.WriteLine();
            }

            bool anyRestricted = false;
            int accessCount = 0;
            DateTime start = DateTime.Now;
            var filesToAccess = stealth ? fileSystem.Take(fileSystem.Count / 2).ToList() : fileSystem.ToList();

            foreach (var file in filesToAccess)
            {
                accessCount++;
                Thread.Sleep(stealth ? 150 : 90);
                bool hasAccess = RoleLevel(currentRole) >= RoleLevel(file.Value);

                if (hasAccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [ACCESS] " + file.Key.PadRight(35) + " → ALLOWED  (" + currentRole + ")");
                    Console.ResetColor();
                    if (currentRole == "admin" && file.Key.Contains("config"))
                    {
                        Thread.Sleep(100);
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine("           > <db_host>192.168.10.100</db_host>");
                        Console.WriteLine("           > <db_pass>F!n@nce_DB_2024</db_pass>");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [ACCESS] " + file.Key.PadRight(35) + " → DENIED   (requires: " + file.Value + ")");
                    Console.ResetColor();
                    anyRestricted = true;
                    bool isCritical = file.Key.Contains("shadow") || file.Key.Contains("credentials");
                    if (diff.IDSActive && isCritical && rng.NextDouble() < (repeatOffender ? 1.0 : 0.70))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("  [IDS ALERT] Critical path access attempt — incident logged");
                        Console.ResetColor();
                        Thread.Sleep(400);
                    }
                }
            }

            double duration = (DateTime.Now - start).TotalSeconds;

            EventLog log = new EventLog();
            log.Timestamp = DateTime.Now; log.SourceIP = sourceIP; log.TargetSystem = targetName;
            log.AttackType = AttackType.FileAccess; log.RestrictedAccess = anyRestricted ? 1 : 0;
            log.AttemptCount = accessCount; log.AttackDuration = (float)duration;
            log.AttackSuccess = privilegeEscalated || alreadyElevated; log.IsMalicious = anyRestricted;
            log.RequestRate = 0; log.PortCount = 0; log.PatternFlag = 0;
            log.OpenPortsFound = 0; log.LockoutTriggered = 0; log.WAFBypassed = 0;
            return log;
        }

        // ── Yardımcılar ───────────────────────────────────────────────────────
        private string[] GetTargetTables(string targetName)
        {
            if (targetName == "Finance Server") return new[] { "customers", "credit_cards", "transactions", "accounts" };
            if (targetName == "Authentication Server") return new[] { "users", "sessions", "tokens", "credentials" };
            return new[] { "access_logs", "proxy_rules", "rate_limits", "blocked_ips" };
        }

        private void PrintExtractedData(string targetName, string table)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("\n  [EXTRACTED — " + table.ToUpper() + "]");
            if (targetName == "Finance Server")
            {
                Console.WriteLine("  ┌────┬──────────────────┬──────────────────────────┬──────────────────┐");
                Console.WriteLine("  │ ID │ Name             │ Email                    │ Card / Balance   │");
                Console.WriteLine("  ├────┼──────────────────┼──────────────────────────┼──────────────────┤");
                Console.WriteLine("  │  1 │ John Smith       │ jsmith@example.com       │ ****-1234 $124k  │");
                Console.WriteLine("  │  2 │ Emma Wilson      │ ewilson@corp.com         │ ****-5678  $89k  │");
                Console.WriteLine("  │  3 │ Michael Brown    │ mbrown@mail.com          │ ****-9012 $267k  │");
                Console.WriteLine("  └────┴──────────────────┴──────────────────────────┴──────────────────┘");
                Console.WriteLine("  [+] 1,247 financial records extracted");
            }
            else if (targetName == "Authentication Server")
            {
                Console.WriteLine("  ┌──────────────┬──────────────────────────────────────┬──────────────────┐");
                Console.WriteLine("  │ User         │ Token                                │ Expiry           │");
                Console.WriteLine("  ├──────────────┼──────────────────────────────────────┼──────────────────┤");
                Console.WriteLine("  │ admin        │ eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOi...  │ 2024-12-31 23:59 │");
                Console.WriteLine("  │ ldapuser     │ eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOi...  │ 2024-12-15 18:00 │");
                Console.WriteLine("  └──────────────┴──────────────────────────────────────┴──────────────────┘");
                Console.WriteLine("  [+] 342 active session tokens extracted");
            }
            else
            {
                Console.WriteLine("  ┌────────────┬─────────────────┬────────┬───────────────────────┬────────┐");
                Console.WriteLine("  │ Time       │ Source IP       │ Method │ Path                  │ Status │");
                Console.WriteLine("  ├────────────┼─────────────────┼────────┼───────────────────────┼────────┤");
                Console.WriteLine("  │ 14:02:11   │ 203.0.113.45    │ GET    │ /api/v1/users         │ 200    │");
                Console.WriteLine("  │ 14:02:18   │ 198.51.100.22   │ POST   │ /admin/login          │ 403    │");
                Console.WriteLine("  └────────────┴─────────────────┴────────┴───────────────────────┴────────┘");
                Console.WriteLine("  [+] 48,291 nginx access log entries extracted");
            }
            Console.ResetColor();
        }

        private int RoleLevel(string role)
        {
            if (role == "root") return 4; if (role == "admin") return 3;
            if (role == "user") return 2; return 1;
        }

        private EventLog OfflineLog(string targetName, AttackType type, IPTracker tracker)
        {
            string ip = tracker.GetOrNewIP(new Random(), 0);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\n  [!!!] " + targetName + " is OFFLINE — attack aborted");
            Console.ResetColor();
            Thread.Sleep(800);
            return new EventLog { Timestamp = DateTime.Now, SourceIP = ip, TargetSystem = targetName, AttackType = type };
        }
    }
}