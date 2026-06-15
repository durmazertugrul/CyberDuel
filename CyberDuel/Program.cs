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

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("  Generating synthetic training data...");
Console.ResetColor();

SyntheticDataGenerator generator = new SyntheticDataGenerator();
List<EventLog> trainingData = generator.Generate(600);
generator.SaveToCsv(trainingData, "training_data.csv");
Console.WriteLine("  600 samples generated → training_data.csv");
Console.WriteLine();

MLDetector mlDetector = new MLDetector();
mlDetector.Train(trainingData);
Console.WriteLine();

RuleEngine ruleEngine = new RuleEngine();
RiskScorer riskScorer = new RiskScorer();
MetricsReporter metrics = new MetricsReporter();
AttackSimulator simulator = new AttackSimulator();

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("  Press any key to begin...");
Console.ResetColor();
Console.ReadKey(true);

bool running = true;

while (running)
{
    Console.Clear();
    PrintBanner();

    // Hedef sistem seç
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  SELECT TARGET SYSTEM:");
    Console.WriteLine();
    Console.WriteLine("  [1]  Finance Server          — " + new FinanceServer().Description);
    Console.WriteLine("  [2]  Authentication Server   — " + new AuthServer().Description);
    Console.WriteLine("  [3]  Public Web Gateway      — " + new WebGateway().Description);
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  Your choice: ");
    Console.ResetColor();

    string targetChoice = Console.ReadLine();

    // Seçilen hedef sistemi hazırla
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
    bool missionRunning = true;
    int round = 0;

    while (missionRunning)
    {
        round++;
        Console.Clear();
        PrintMissionHeader(targetName, missionText, round, metrics);

        Console.WriteLine("  [ ATTACK PANEL ]");
        Console.WriteLine("  [1]  Port Scan              — Map open ports and services");
        Console.WriteLine("  [2]  Brute Force            — Dictionary attack on user accounts");
        Console.WriteLine("  [3]  DDoS Flood             — Overwhelm server with traffic");
        Console.WriteLine("  [4]  SQL Injection          — Inject malicious database queries");
        Console.WriteLine("  [5]  Unauthorized Access    — Attempt restricted file access");
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

        // Saldırıyı çalıştır
        EventLog log = null;

        Console.Clear();

        if (input == "1")
            log = simulator.DoPortScan(targetName, portTable);
        else if (input == "2")
            log = simulator.DoBruteForce(targetName, userAccounts);
        else if (input == "3")
            log = simulator.DoDDoS(targetName, maxCapacity);
        else if (input == "4")
            log = simulator.DoSqlInjection(targetName, hasWAF);
        else if (input == "5")
            log = simulator.DoFileAccess(targetName, fileSystem);
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Invalid selection.");
            Console.ResetColor();
            Thread.Sleep(1000);
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
        metrics.Add(detected, log.IsMalicious);

        // Tespit sonucunu göster
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

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.WriteLine("                         CYBERDUEL");
    Console.WriteLine("              CONSOLE-BASED AI CYBERSECURITY SIMULATION");
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.ResetColor();
}

static void PrintMissionHeader(string name, string mission, int round, MetricsReporter m)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.WriteLine("  TARGET : " + name);
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  " + mission);
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ──────────────────────────────────────────────────────────");
    Console.WriteLine("  Round: " + round + "   Events: " + m.Total() +
                      "   Precision: " + m.GetPrecision().ToString("F2") +
                      "   Recall: " + m.GetRecall().ToString("F2") +
                      "   F1: " + m.GetF1().ToString("F2"));
    Console.WriteLine("  ══════════════════════════════════════════════════════════");
    Console.ResetColor();
    Console.WriteLine();
}

static void PrintDetectionResult(EventLog log, bool detected)
{
    ConsoleColor threatColor = log.ThreatLevel switch
    {
        ThreatLevel.Low => ConsoleColor.Green,
        ThreatLevel.Moderate => ConsoleColor.Yellow,
        ThreatLevel.High => ConsoleColor.DarkYellow,
        ThreatLevel.Critical => ConsoleColor.Red,
        _ => ConsoleColor.White
    };

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ──────────────────────────────────────────────────────────");
    Console.WriteLine("  [ DETECTION RESULT ]");
    Console.WriteLine("  Time        : " + log.Timestamp.ToString("HH:mm:ss"));
    Console.WriteLine("  Source IP   : " + log.SourceIP);
    Console.WriteLine("  Attack Type : " + log.AttackType);
    Console.WriteLine("  Attack Outcome: " + (log.AttackSuccess ? "SUCCESS" : "FAILED"));
    Console.ResetColor();

    Console.ForegroundColor = log.RuleDetected ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine("  Rule Engine : " + (log.RuleDetected ? "FLAGGED" : "CLEAN"));
    Console.ResetColor();

    Console.ForegroundColor = log.MLDetected ? ConsoleColor.Red : ConsoleColor.Green;
    Console.WriteLine("  ML Model    : " + (log.MLDetected ? "FLAGGED" : "CLEAN") +
                      "   (Probability: " + log.MLProbability.ToString("F2") + ")");
    Console.ResetColor();

    Console.ForegroundColor = threatColor;
    Console.WriteLine("  Risk Score  : " + log.RiskScore.ToString("F2"));
    Console.WriteLine("  Threat Level: " + log.ThreatLevel);

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