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

// ── Başlangıç ────────────────────────────────────────────────────────────────
PrintBanner();

// Zorluk seç
DifficultySettings difficulty = SelectDifficulty();
Console.Clear();
PrintBanner();

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("\n  [*] Difficulty: " + difficulty.Name);
Console.WriteLine("  [*] Generating synthetic training data...");
Console.ResetColor();

SyntheticDataGenerator generator = new SyntheticDataGenerator();
List<EventLog> trainingData = generator.Generate(600);
generator.SaveToCsv(trainingData, "training_data.csv");
Console.WriteLine("  [*] 600 samples generated → training_data.csv");
Console.WriteLine();

MLDetector mlDetector = new MLDetector();
mlDetector.Train(trainingData);
Console.WriteLine();

RuleEngine ruleEngine = new RuleEngine();
RiskScorer riskScorer = new RiskScorer();
MetricsReporter metrics = new MetricsReporter();
MissionScorer scorer = new MissionScorer();
AttackSimulator simulator = new AttackSimulator();
SessionState sessionState = new SessionState();

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("  Press any key to begin...");
Console.ResetColor();
Console.ReadKey(true);

// ── Ana döngü ─────────────────────────────────────────────────────────────────
bool running = true;

while (running)
{
    Console.Clear();
    PrintBanner();

    // Hedef sistem seç
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  SELECT TARGET SYSTEM:\n");
    Console.WriteLine("  [1]  Finance Server          — " + new FinanceServer().Description);
    Console.WriteLine("  [2]  Authentication Server   — " + new AuthServer().Description);
    Console.WriteLine("  [3]  Public Web Gateway      — " + new WebGateway().Description);
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  Your choice: ");
    Console.ResetColor();

    string targetChoice = Console.ReadLine();

    string targetName = "";
    string missionText = "";
    Dictionary<int, (string Service, string Status)> portTable = null;
    Dictionary<string, string> fileSystem = null;
    Dictionary<string, string> userAccounts = null;
    bool hasWAF = false;
    int maxCapacity = 0;

    if (targetChoice == "1")
    {
        FinanceServer fs = new FinanceServer();
        targetName = fs.Name;
        missionText = fs.GetMission();
        portTable = fs.PortTable;
        fileSystem = fs.FileSystem;
        userAccounts = fs.UserAccounts;
        hasWAF = fs.HasWAF;
        maxCapacity = fs.MaxCapacity;
    }
    else if (targetChoice == "2")
    {
        AuthServer auth = new AuthServer();
        targetName = auth.Name;
        missionText = auth.GetMission();
        portTable = auth.PortTable;
        fileSystem = auth.FileSystem;
        userAccounts = auth.UserAccounts;
        hasWAF = auth.HasWAF;
        maxCapacity = auth.MaxCapacity;
    }
    else
    {
        WebGateway gw = new WebGateway();
        targetName = gw.Name;
        missionText = gw.GetMission();
        portTable = gw.PortTable;
        fileSystem = gw.FileSystem;
        userAccounts = gw.UserAccounts;
        hasWAF = gw.HasWAF;
        maxCapacity = gw.MaxCapacity;
    }

    metrics.Reset();
    scorer.Reset();
    sessionState.Reset();
    bool missionRunning = true;
    int round = 0;

    while (missionRunning)
    {
        round++;
        Console.Clear();
        PrintMissionHeader(targetName, missionText, round, metrics, scorer, difficulty, sessionState);

        // Sunucu offline uyarısı
        if (sessionState.ServerOffline)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("  [!!!] TARGET SERVER IS OFFLINE\n");
            Console.ResetColor();
        }

        // Önceki saldırı bilgisi
        if (sessionState.AdminAccessGained)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [+] Admin credentials active: " +
                sessionState.CompromisedUsername + " / " + sessionState.CompromisedPassword);
            Console.ResetColor();
        }
        if (sessionState.DiscoveredOpenPorts.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  [+] Known open ports: ");
            Console.WriteLine(string.Join(", ", sessionState.DiscoveredOpenPorts));
            Console.ResetColor();
        }
        if (sessionState.DatabaseBreached)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("  [+] Database breached: " + sessionState.BreachedTable + " table extracted");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine("  [ ATTACK PANEL ]");
        Console.WriteLine("  [1]  Port Scan           +50  — Map open ports and services");
        Console.WriteLine("  [2]  Brute Force         +100 — Dictionary attack on accounts");
        Console.WriteLine("  [3]  DDoS Flood          +200 — Overwhelm server capacity");
        Console.WriteLine("  [4]  SQL Injection       +175 — Inject malicious DB queries");
        Console.WriteLine("  [5]  File Access         +150 — Attempt restricted path access");
        Console.WriteLine("  [0]  End Mission");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  Select attack: ");
        Console.ResetColor();

        string input = Console.ReadLine();

        if (input == "0")
        {
            missionRunning = false;
            break;
        }

        Console.Clear();
        EventLog log = null;

        if (input == "1")
            log = simulator.DoPortScan(targetName, portTable, sessionState, difficulty);
        else if (input == "2")
            log = simulator.DoBruteForce(targetName, userAccounts, sessionState, difficulty);
        else if (input == "3")
            log = simulator.DoDDoS(targetName, maxCapacity, sessionState, difficulty);
        else if (input == "4")
            log = simulator.DoSqlInjection(targetName, hasWAF, sessionState, difficulty);
        else if (input == "5")
            log = simulator.DoFileAccess(targetName, fileSystem, sessionState, difficulty);
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Invalid selection.");
            Console.ResetColor();
            Thread.Sleep(1000);
            continue;
        }

        // Boş log (offline target) ise atla
        if (log.AttackType == AttackType.Normal) { Console.ReadLine(); continue; }

        // Tespit katmanları
        log.RuleDetected = ruleEngine.Check(log);
        float mlProb = 0;
        log.MLDetected = mlDetector.Predict(log, out mlProb);
        log.MLProbability = mlProb;

        log.RiskScore = riskScorer.Calculate(log.RuleDetected, log.MLProbability);
        log.ThreatLevel = riskScorer.GetLevel(log.RiskScore);

        bool detected = log.RuleDetected || log.MLDetected;
        metrics.Add(detected, log.IsMalicious);
        sessionState.AllEvents.Add(log);

        // Skorlama ve bonus kontrolleri
        scorer.RecordAttack(log.AttackSuccess, detected);

        if (input == "3" && sessionState.ServerOffline)
            scorer.AddServerDownBonus();
        if (input == "2" && sessionState.AdminAccessGained && log.AttackSuccess)
            scorer.AddAdminAccessBonus();
        if (input == "4" && sessionState.DatabaseBreached && log.AttackSuccess)
            scorer.AddDatabaseBreachBonus();

        PrintDetectionResult(log, detected);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Press Enter to continue...");
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

    metrics.PrintAll();
    scorer.PrintSummary();

    // Session log export
    ExportSessionLog(sessionState, targetName, scorer.Score);

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  Start a new mission? (y/n): ");
    Console.ResetColor();
    string again = Console.ReadLine();

    if (again != "y" && again != "Y")
        running = false;
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n  CyberDuel session ended. Goodbye.\n");
Console.ResetColor();

// ── Yardımcı fonksiyonlar ─────────────────────────────────────────────────────

static DifficultySettings SelectDifficulty()
{
    PrintBanner();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  SELECT DIFFICULTY:\n");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  [1]  EASY   — No IDS intervention, weak WAF, password always in wordlist");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  [2]  MEDIUM — IDS active, standard WAF, 40% chance password missing");
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("  [3]  HARD   — Aggressive IDS, strong WAF, 60% chance password missing");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  Your choice: ");
    Console.ResetColor();

    string choice = Console.ReadLine();

    if (choice == "1") return DifficultySettings.Easy();
    if (choice == "3") return DifficultySettings.Hard();
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
    MetricsReporter m, MissionScorer s, DifficultySettings d, SessionState state)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.WriteLine("  TARGET     : " + name + "  [" + d.Name + "]");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  " + mission);
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ──────────────────────────────────────────────────────────");
    Console.WriteLine("  Round: " + round +
                      "  |  Events: " + m.Total() +
                      "  |  P: " + m.GetPrecision().ToString("F2") +
                      "  R: " + m.GetRecall().ToString("F2") +
                      "  F1: " + m.GetF1().ToString("F2") +
                      "  |  Score: " + s.Score + " pts");
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.ResetColor();
    Console.WriteLine();
}

static void PrintDetectionResult(EventLog log, bool detected)
{
    ConsoleColor threatColor = ConsoleColor.White;
    if (log.ThreatLevel == ThreatLevel.Low) threatColor = ConsoleColor.Green;
    else if (log.ThreatLevel == ThreatLevel.Moderate) threatColor = ConsoleColor.Yellow;
    else if (log.ThreatLevel == ThreatLevel.High) threatColor = ConsoleColor.DarkYellow;
    else if (log.ThreatLevel == ThreatLevel.Critical) threatColor = ConsoleColor.Red;

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ──────────────────────────────────────────────────────────");
    Console.WriteLine("  [ DETECTION RESULT ]");
    Console.WriteLine("  Time           : " + log.Timestamp.ToString("HH:mm:ss"));
    Console.WriteLine("  Source IP      : " + log.SourceIP);
    Console.WriteLine("  Attack Type    : " + log.AttackType);
    Console.WriteLine("  Attack Outcome : " + (log.AttackSuccess ? "SUCCESS" : "FAILED"));
    Console.ResetColor();

    Console.ForegroundColor = log.RuleDetected ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine("  Rule Engine    : " + (log.RuleDetected ? "FLAGGED" : "CLEAN"));
    Console.ResetColor();

    Console.ForegroundColor = log.MLDetected ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine("  ML Model       : " + (log.MLDetected ? "FLAGGED" : "CLEAN") +
                      "  (Probability: " + log.MLProbability.ToString("F2") + ")");
    Console.ResetColor();

    Console.ForegroundColor = threatColor;
    Console.WriteLine("  Risk Score     : " + log.RiskScore.ToString("F2"));
    Console.WriteLine("  Threat Level   : " + log.ThreatLevel);

    if (detected)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n  >>> ATTACK DETECTED — IDS ALERT RAISED <<<");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("\n  --- No threat detected ---");
    }

    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ──────────────────────────────────────────────────────────");
    Console.ResetColor();
}

static void ExportSessionLog(SessionState state, string targetName, int score)
{
    if (state.AllEvents.Count == 0) return;

    string filename = "session_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";

    List<string> lines = new List<string>();
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
        bool last = (i == state.AllEvents.Count - 1);
        bool detected = e.RuleDetected || e.MLDetected;

        lines.Add("    {");
        lines.Add("      \"time\": \"" + e.Timestamp.ToString("HH:mm:ss") + "\",");
        lines.Add("      \"attackType\": \"" + e.AttackType + "\",");
        lines.Add("      \"sourceIP\": \"" + e.SourceIP + "\",");
        lines.Add("      \"riskScore\": " + e.RiskScore.ToString("F2") + ",");
        lines.Add("      \"threatLevel\": \"" + e.ThreatLevel + "\",");
        lines.Add("      \"ruleDetected\": " + (e.RuleDetected ? "true" : "false") + ",");
        lines.Add("      \"mlDetected\": " + (e.MLDetected ? "true" : "false") + ",");
        lines.Add("      \"mlProbability\": " + e.MLProbability.ToString("F2") + ",");
        lines.Add("      \"attackSuccess\": " + (e.AttackSuccess ? "true" : "false"));
        lines.Add("    }" + (last ? "" : ","));
    }

    lines.Add("  ]");
    lines.Add("}");

    File.WriteAllLines(filename, lines);

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("\n  [*] Session log exported → " + filename);
    Console.ResetColor();
}