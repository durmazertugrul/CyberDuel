using System.Threading;
using CyberDuel.Attacks;
using CyberDuel.Data;
using CyberDuel.Detection;
using CyberDuel.Evaluation;
using CyberDuel.Models;
using CyberDuel.Scoring;
using CyberDuel.Systems;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.CursorVisible = false;

PrintBanner();
DifficultySettings difficulty = SelectDifficulty();
Console.Clear(); PrintBanner();

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("\n  [*] Difficulty: " + difficulty.Name);
Console.WriteLine("  [*] Generating synthetic training data...");
Console.ResetColor();

SyntheticDataGenerator generator = new SyntheticDataGenerator();
List<EventLog> trainingData = generator.Generate(600);
generator.SaveToCsv(trainingData, "training_data.csv");
Console.WriteLine("  [*] 600 samples generated → training_data.csv\n");

MLDetector mlDetector = new MLDetector();
mlDetector.Train(trainingData);
Console.WriteLine();

RuleEngine ruleEngine = new RuleEngine();
RiskScorer riskScorer = new RiskScorer();
MetricsReporter metrics = new MetricsReporter();
MissionScorer scorer = new MissionScorer();
AttackSimulator simulator = new AttackSimulator();
SessionState sessionState = new SessionState();
IPTracker ipTracker = new IPTracker();
List<SessionRecord> history = new List<SessionRecord>();
int missionNumber = 0;

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("  Press any key to begin...");
Console.ResetColor();
Console.ReadKey(true);

bool running = true;
while (running)
{
    missionNumber++;
    Console.Clear(); PrintBanner();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  SELECT TARGET SYSTEM:\n");

    if (sessionState.CompromisedTargets.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  [LATERAL] Previously compromised: " + string.Join(", ", sessionState.CompromisedTargets));
        Console.ResetColor(); Console.WriteLine();
    }

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  [1]  Finance Server          — " + new FinanceServer().Description);
    Console.WriteLine("  [2]  Authentication Server   — " + new AuthServer().Description);
    Console.WriteLine("  [3]  Public Web Gateway      — " + new WebGateway().Description);
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  Your choice: ");
    Console.ResetColor();

    string targetChoice = Console.ReadLine();
    string targetName = ""; string missionText = "";
    Dictionary<int, (string, string)> portTable = null;
    Dictionary<string, string> fileSystem = null, userAccounts = null;
    bool hasWAF = false; int maxCapacity = 0;

    if (targetChoice == "1") { var t = new FinanceServer(); targetName = t.Name; missionText = t.GetMission(); portTable = t.PortTable; fileSystem = t.FileSystem; userAccounts = t.UserAccounts; hasWAF = t.HasWAF; maxCapacity = t.MaxCapacity; }
    else if (targetChoice == "2") { var t = new AuthServer(); targetName = t.Name; missionText = t.GetMission(); portTable = t.PortTable; fileSystem = t.FileSystem; userAccounts = t.UserAccounts; hasWAF = t.HasWAF; maxCapacity = t.MaxCapacity; }
    else { var t = new WebGateway(); targetName = t.Name; missionText = t.GetMission(); portTable = t.PortTable; fileSystem = t.FileSystem; userAccounts = t.UserAccounts; hasWAF = t.HasWAF; maxCapacity = t.MaxCapacity; }

    bool lateralBonus = sessionState.HasLateralAccess(targetName);
    if (lateralBonus)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n  [LATERAL MOVEMENT] Internal network access from previous target");
        Console.WriteLine("  [+] Attack success rates elevated");
        Console.ResetColor(); Thread.Sleep(800);
    }

    DifficultySettings effectiveDiff = lateralBonus ? ApplyLateralBonus(difficulty) : difficulty;

    metrics.Reset(); scorer.Reset();
    sessionState.ServerOffline = false; sessionState.AdminAccessGained = false;
    sessionState.CompromisedUsername = ""; sessionState.CompromisedPassword = "";
    sessionState.DatabaseBreached = false; sessionState.BreachedTable = "";
    sessionState.AllEvents.Clear(); sessionState.DiscoveredOpenPorts.Clear();
    sessionState.RuleTP = sessionState.RuleFP = sessionState.RuleFN = sessionState.RuleTN = 0;
    sessionState.MLTP = sessionState.MLFP = sessionState.MLFN = sessionState.MLTN = 0;
    sessionState.PreBlockedCount = 0; sessionState.RecentAttacks.Clear();
    ipTracker.Reset();

    bool missionRunning = true; int round = 0;

    while (missionRunning)
    {
        round++;
        Console.Clear();
        PrintMissionHeader(targetName, missionText, round, metrics, scorer, effectiveDiff, sessionState);

        if (sessionState.ServerOffline) { Console.ForegroundColor = ConsoleColor.DarkRed; Console.WriteLine("  [!!!] SERVER OFFLINE\n"); Console.ResetColor(); }
        if (sessionState.AdminAccessGained) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("  [+] Admin: " + sessionState.CompromisedUsername + " / " + sessionState.CompromisedPassword); Console.ResetColor(); }
        if (sessionState.DiscoveredOpenPorts.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            bool ssh = sessionState.DiscoveredOpenPorts.Contains(22);
            bool db = sessionState.DiscoveredOpenPorts.Contains(1433) || sessionState.DiscoveredOpenPorts.Contains(3306);
            Console.Write("  [+] Open ports: " + string.Join(", ", sessionState.DiscoveredOpenPorts));
            if (ssh) Console.Write("  [SSH TARGET]");
            if (db) Console.Write("  [DB ENHANCED]");
            Console.WriteLine(); Console.ResetColor();
        }
        if (sessionState.DatabaseBreached) { Console.ForegroundColor = ConsoleColor.Magenta; Console.WriteLine("  [+] DB breached: " + sessionState.BreachedTable); Console.ResetColor(); }
        if (ipTracker.BannedCount > 0) { Console.ForegroundColor = ConsoleColor.DarkRed; Console.WriteLine("  [IDS] " + ipTracker.BannedCount + " IP(s) banned"); Console.ResetColor(); }

        Console.WriteLine();
        Console.WriteLine("  [ ATTACK PANEL ]");
        Console.WriteLine("  [1]  Port Scan");
        Console.WriteLine("  [2]  Brute Force      " + (sessionState.DiscoveredOpenPorts.Contains(22) ? "[SSH TARGET]" : ""));
        Console.WriteLine("  [3]  DDoS Flood");
        Console.WriteLine("  [4]  SQL Injection    " + (sessionState.DiscoveredOpenPorts.Contains(1433) ? "[DB ENHANCED]" : ""));
        Console.WriteLine("  [5]  File Access      " + (sessionState.AdminAccessGained ? "[ADMIN]" : ""));
        Console.WriteLine("  [0]  End Mission");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  Select attack: ");
        Console.ResetColor();

        string input = Console.ReadLine();
        if (input == "0") { missionRunning = false; break; }

        if (input != "1" && input != "2" && input != "3" && input != "4" && input != "5")
        {
            Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("  Invalid selection."); Console.ResetColor();
            Thread.Sleep(800); continue;
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Mode [n] Normal / [s] Stealth: ");
        Console.ResetColor();
        bool stealth = Console.ReadLine()?.Trim().ToLower() == "s";

        Console.Clear();
        EventLog log = null;

        if (input == "1") log = simulator.DoPortScan(targetName, portTable, sessionState, effectiveDiff, ipTracker, stealth);
        else if (input == "2") log = simulator.DoBruteForce(targetName, userAccounts, sessionState, effectiveDiff, ipTracker, stealth);
        else if (input == "3") log = simulator.DoDDoS(targetName, maxCapacity, sessionState, effectiveDiff, ipTracker, stealth);
        else if (input == "4") log = simulator.DoSqlInjection(targetName, hasWAF, sessionState, effectiveDiff, ipTracker, stealth);
        else log = simulator.DoFileAccess(targetName, fileSystem, sessionState, effectiveDiff, ipTracker, stealth);

        if (log.AttackType == AttackType.Normal) { Console.ReadLine(); continue; }

        // Banlı IP kontrolü — rule/ML pipeline'a girmez, metrikleri bozmaz
        if (ipTracker.IsBanned(log.SourceIP))
        {
            sessionState.PreBlockedCount++;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\n  [IDS] BANNED IP: " + log.SourceIP + " — attack automatically blocked");
            Console.ResetColor();
            scorer.RecordAttack(false, true);
            Console.Write("\n  Press Enter to continue...");
            Console.ReadLine();
            continue;
        }

        // Tespit katmanları
        log.RuleDetected = ruleEngine.Check(log);
        float mlProb = 0;
        log.MLDetected = mlDetector.Predict(log, out mlProb);
        log.MLProbability = mlProb;
        log.RiskScore = riskScorer.Calculate(log.RuleDetected, log.MLProbability);
        log.ThreatLevel = riskScorer.GetLevel(log.RiskScore);

        bool detected = log.RuleDetected || log.MLDetected;
        metrics.Add(detected, log.IsMalicious, log.AttackType);
        sessionState.RecordLayerResults(log.RuleDetected, log.MLDetected, log.IsMalicious);
        sessionState.AllEvents.Add(log);
        sessionState.RecordAttack(log.AttackType);

        if (detected) ipTracker.RecordDetection(log.SourceIP);

        scorer.RecordAttack(log.AttackSuccess, detected);
        if (input == "3" && sessionState.ServerOffline) scorer.AddServerDownBonus();
        if (input == "2" && log.AttackSuccess) scorer.AddAdminAccessBonus();
        if (input == "4" && log.AttackSuccess) scorer.AddDatabaseBreachBonus();

        if (log.AttackSuccess && (input == "2" || input == "4"))
            sessionState.CompromisedTargets.Add(targetName);

        string patternAlert = "";
        if (sessionState.IsCoordinatedAttack()) patternAlert = "[IDS] COORDINATED ATTACK — multiple attack types in 90s";
        else if (sessionState.IsRepeatedPattern()) patternAlert = "[IDS] REPEATED PATTERN — same attack type 3x consecutively";

        string mlExplanation = mlDetector.ExplainDecision(log, log.MLProbability);
        string missReason = "";
        if (log.IsMalicious && !detected) missReason = mlDetector.ExplainMiss(log);

        PrintDetectionResult(log, detected, mlExplanation, missReason, patternAlert);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("\n  Press Enter to continue...");
        Console.ResetColor();
        Console.ReadLine();
    }

    // Görev özeti
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.WriteLine("  MISSION COMPLETE — " + targetName);
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.ResetColor();

    SIEMTimeline.Print(sessionState.AllEvents, targetName);
    sessionState.PrintLayerComparison();
    metrics.PrintAll();
    scorer.PrintSummary();
    ExportSessionLog(sessionState, targetName, scorer.Score);

    // Replay seçeneği — görev bittikten sonra, önceki loglar anlamlıysa
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  Replay a previous session log? (y/n): ");
    Console.ResetColor();
    if (Console.ReadLine()?.Trim().ToLower() == "y")
        ReplayAnalyzer.Run(mlDetector);

    var layerF1s = sessionState.GetLayerF1s();
    history.Add(new SessionRecord
    {
        MissionNumber = missionNumber,
        TargetName = targetName,
        Difficulty = effectiveDiff.Name,
        Score = scorer.Score,
        Precision = metrics.GetPrecision(),
        Recall = metrics.GetRecall(),
        F1 = metrics.GetF1(),
        RuleOnlyF1 = layerF1s.f1Rule,
        MLOnlyF1 = layerF1s.f1ML,
        TotalEvents = metrics.Total()
    });

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  New mission? (y/n): ");
    Console.ResetColor();
    running = Console.ReadLine()?.Trim().ToLower() == "y";
}

if (history.Count > 1) PrintSessionHistory(history);

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n  CyberDuel session ended. Goodbye.\n");
Console.ResetColor();

// ── Yardımcı fonksiyonlar ─────────────────────────────────────────────────────

static DifficultySettings ApplyLateralBonus(DifficultySettings base_) => new DifficultySettings
{
    Name = base_.Name + "+LATERAL",
    WAFBlockRate = base_.WAFBlockRate * 0.70,
    LockoutThreshold = base_.LockoutThreshold + 5,
    CapacityMultiplier = base_.CapacityMultiplier * 1.20,
    PasswordMissingChance = base_.PasswordMissingChance * 0.50,
    IDSActive = base_.IDSActive,
    InjectionSuccessRate = base_.InjectionSuccessRate + 0.20
};

static DifficultySettings SelectDifficulty()
{
    PrintBanner();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.White; Console.WriteLine("  SELECT DIFFICULTY:\n");
    Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("  [1]  EASY   — No IDS, weak WAF, password always in wordlist");
    Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("  [2]  MEDIUM — IDS active, standard WAF, 40% chance password missing");
    Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("  [3]  HARD   — Aggressive IDS, strong WAF, 60% chance password missing");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow; Console.Write("  Your choice: "); Console.ResetColor();
    string c = Console.ReadLine();
    if (c == "1") return DifficultySettings.Easy();
    if (c == "3") return DifficultySettings.Hard();
    return DifficultySettings.Medium();
}

static void PrintBanner()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.WriteLine("                         CYBERDUEL");
    Console.WriteLine("              CONSOLE-BASED AI CYBERSECURITY SIMULATION");
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.ResetColor();
}

static void PrintMissionHeader(string name, string mission, int round,
    MetricsReporter m, MissionScorer s, DifficultySettings d, SessionState st)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.WriteLine("  TARGET : " + name + "  [" + d.Name + "]");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("  " + mission); Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ──────────────────────────────────────────────────────────");
    Console.WriteLine("  Round: " + round + "  Events: " + m.Total() + "  P: " + m.GetPrecision().ToString("F2") + "  R: " + m.GetRecall().ToString("F2") + "  F1: " + m.GetF1().ToString("F2") + "  Score: " + s.Score + " pts");
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.ResetColor(); Console.WriteLine();
}

static void PrintDetectionResult(EventLog log, bool detected,
    string mlExplanation, string missReason, string patternAlert)
{
    ConsoleColor tc = log.ThreatLevel == ThreatLevel.Critical ? ConsoleColor.Red :
                      log.ThreatLevel == ThreatLevel.High ? ConsoleColor.DarkYellow :
                      log.ThreatLevel == ThreatLevel.Moderate ? ConsoleColor.Yellow : ConsoleColor.Green;
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ──────────────────────────────────────────────────────────");
    Console.WriteLine("  [ DETECTION RESULT ]");
    Console.WriteLine("  Time        : " + log.Timestamp.ToString("HH:mm:ss"));
    Console.WriteLine("  Source IP   : " + log.SourceIP);
    Console.WriteLine("  Attack      : " + log.AttackType + (log.AttackSuccess ? " (SUCCESS)" : " (FAILED)"));
    Console.ResetColor();
    Console.ForegroundColor = log.RuleDetected ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine("  Rule Engine : " + (log.RuleDetected ? "FLAGGED" : "CLEAN")); Console.ResetColor();
    Console.ForegroundColor = log.MLDetected ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine("  ML Model    : " + (log.MLDetected ? "FLAGGED" : "CLEAN") + "  (p=" + log.MLProbability.ToString("F2") + ")"); Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ML Reason   : " + mlExplanation); Console.ResetColor();
    if (!string.IsNullOrEmpty(missReason)) { Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine("  MISS Reason : " + missReason); Console.ResetColor(); }
    Console.ForegroundColor = tc;
    Console.WriteLine("  Risk Score  : " + log.RiskScore.ToString("F2") + "  |  Threat: " + log.ThreatLevel);
    Console.WriteLine(detected ? "\n  >>> ATTACK DETECTED — IDS ALERT RAISED <<<" : "\n  --- No threat detected ---");
    Console.ResetColor();
    if (!string.IsNullOrEmpty(patternAlert)) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("  " + patternAlert); Console.ResetColor(); }
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ──────────────────────────────────────────────────────────");
    Console.ResetColor();
}

static void PrintSessionHistory(List<SessionRecord> history)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  [ SESSION HISTORY ]\n");
    Console.WriteLine("  ┌───────┬───────────────────────┬──────────────┬───────┬────────┬──────────┬─────────┐");
    Console.WriteLine("  │  #    │ Target                │ Difficulty   │ Score │ F1     │ Rule F1  │ ML F1   │");
    Console.WriteLine("  ├───────┼───────────────────────┼──────────────┼───────┼────────┼──────────┼─────────┤");
    foreach (SessionRecord r in history)
    {
        Console.ForegroundColor = r.Score >= 450 ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine("  │  " + r.MissionNumber.ToString().PadRight(4) + " │ " + r.TargetName.PadRight(21) + " │ " + r.Difficulty.PadRight(12) + " │ " + r.Score.ToString().PadLeft(5) + " │ " + r.F1.ToString("F2").PadLeft(6) + " │ " + r.RuleOnlyF1.ToString("F2").PadLeft(8) + " │ " + r.MLOnlyF1.ToString("F2").PadLeft(7) + " │");
    }
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  └───────┴───────────────────────┴──────────────┴───────┴────────┴──────────┴─────────┘");
    Console.ResetColor();
}

static void ExportSessionLog(SessionState state, string targetName, int score)
{
    if (state.AllEvents.Count == 0) return;
    string fn = "session_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
    var lines = new List<string>();
    lines.Add("{");
    lines.Add("  \"target\": \"" + targetName + "\",");
    lines.Add("  \"timestamp\": \"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",");
    lines.Add("  \"score\": " + score + ",");
    lines.Add("  \"adminAccess\": " + (state.AdminAccessGained ? "true" : "false") + ",");
    lines.Add("  \"serverOffline\": " + (state.ServerOffline ? "true" : "false") + ",");
    lines.Add("  \"databaseBreached\": " + (state.DatabaseBreached ? "true" : "false") + ",");
    lines.Add("  \"events\": [");
    for (int i = 0; i < state.AllEvents.Count; i++)
    {
        EventLog e = state.AllEvents[i];
        bool last = i == state.AllEvents.Count - 1;
        lines.Add("    {");
        lines.Add("      \"time\": \"" + e.Timestamp.ToString("HH:mm:ss") + "\",");
        lines.Add("      \"attack\": \"" + e.AttackType + "\",");
        lines.Add("      \"source\": \"" + e.SourceIP + "\",");
        lines.Add("      \"attemptCount\": " + e.AttemptCount + ",");
        lines.Add("      \"requestRate\": " + e.RequestRate + ",");
        lines.Add("      \"portCount\": " + e.PortCount + ",");
        lines.Add("      \"patternFlag\": " + e.PatternFlag + ",");
        lines.Add("      \"restrictedAccess\": " + e.RestrictedAccess + ",");
        lines.Add("      \"openPortsFound\": " + e.OpenPortsFound + ",");
        lines.Add("      \"lockoutTriggered\": " + e.LockoutTriggered + ",");
        lines.Add("      \"wafBypassed\": " + e.WAFBypassed + ",");
        lines.Add("      \"attackDuration\": " + e.AttackDuration.ToString("F2") + ",");
        lines.Add("      \"riskScore\": " + e.RiskScore.ToString("F2") + ",");
        lines.Add("      \"threatLevel\": \"" + e.ThreatLevel + "\",");
        lines.Add("      \"ruleDetected\": " + (e.RuleDetected ? "true" : "false") + ",");
        lines.Add("      \"originalMLDetected\": " + (e.MLDetected ? "true" : "false") + ",");
        lines.Add("      \"originalMLProbability\": " + e.MLProbability.ToString("F2") + ",");
        lines.Add("      \"success\": " + (e.AttackSuccess ? "true" : "false"));
        lines.Add("    }" + (last ? "" : ","));
    }
    lines.Add("  ]"); lines.Add("}");
    File.WriteAllLines(fn, lines);
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("\n  [*] Session log → " + fn);
    Console.ResetColor();
}