using CyberDuel.Attacks;
using CyberDuel.Data;
using CyberDuel.Detection;
using CyberDuel.Evaluation;
using CyberDuel.Models;
using CyberDuel.Scoring;
using CyberDuel.Systems;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// --- STARTUP ---
Console.WriteLine("======================================================================");
Console.WriteLine("                          CYBERDUEL");
Console.WriteLine("              CONSOLE-BASED CYBERSECURITY SIMULATION");
Console.WriteLine("======================================================================");
Console.WriteLine();
Console.WriteLine("  Generating synthetic training data...");

// Sentetik eğitim verisi üret ve CSV olarak kaydet
SyntheticDataGenerator generator = new SyntheticDataGenerator();
List<EventLog> trainingData = generator.Generate(500);
generator.SaveToCsv(trainingData, "training_data.csv");

Console.WriteLine("  500 samples generated -> training_data.csv");
Console.WriteLine();

// ML modelini eğit
MLDetector mlDetector = new MLDetector();
mlDetector.Train(trainingData);
Console.WriteLine();

// Diğer bileşenleri başlat
RuleEngine ruleEngine = new RuleEngine();
RiskScorer riskScorer = new RiskScorer();
MetricsReporter metrics = new MetricsReporter();
AttackSimulator simulator = new AttackSimulator();

Console.WriteLine("  Press any key to start...");
Console.ReadKey(true);

// --- MAIN LOOP ---
bool running = true;

while (running)
{
    // Hedef sistem seçim ekranı
    Console.Clear();
    Console.WriteLine("======================================================================");
    Console.WriteLine("  SELECT TARGET SYSTEM:");
    Console.WriteLine("----------------------------------------------------------------------");
    Console.WriteLine("  [1] Finance Server        - " + new FinanceServer().Description);
    Console.WriteLine("  [2] Authentication Server - " + new AuthServer().Description);
    Console.WriteLine("  [3] Public Web Gateway    - " + new WebGateway().Description);
    Console.WriteLine("======================================================================");
    Console.Write("  Your choice: ");

    string targetChoice = Console.ReadLine();
    string targetName = "";
    string missionText = "";

    if (targetChoice == "1")
    {
        FinanceServer fs = new FinanceServer();
        targetName = fs.Name;
        missionText = fs.GetMission();
    }
    else if (targetChoice == "2")
    {
        AuthServer auth = new AuthServer();
        targetName = auth.Name;
        missionText = auth.GetMission();
    }
    else
    {
        WebGateway gw = new WebGateway();
        targetName = gw.Name;
        missionText = gw.GetMission();
    }

    metrics.Reset();
    int round = 0;

    // Görev döngüsü
    bool missionRunning = true;

    while (missionRunning)
    {
        round++;
        Console.Clear();
        Console.WriteLine("======================================================================");
        Console.WriteLine("  TARGET : " + targetName);
        Console.WriteLine("  " + missionText);
        Console.WriteLine("----------------------------------------------------------------------");
        Console.WriteLine("  Round: " + round + "  |  Total Events: " + metrics.Total());
        Console.WriteLine("  Precision: " + metrics.GetPrecision().ToString("F2") +
                          "  |  Recall: " + metrics.GetRecall().ToString("F2") +
                          "  |  F1: " + metrics.GetF1().ToString("F2"));
        Console.WriteLine("======================================================================");
        Console.WriteLine();
        Console.WriteLine("  [ ATTACK PANEL ]");
        Console.WriteLine("  [1] Port Scan          - Fast multi-port probing");
        Console.WriteLine("  [2] Brute Force        - Repeated login attempts");
        Console.WriteLine("  [3] DDoS Flood         - High-volume traffic burst");
        Console.WriteLine("  [4] SQL Injection      - Malicious query injection");
        Console.WriteLine("  [5] File Access        - Unauthorized file access");
        Console.WriteLine("  [0] End Mission");
        Console.WriteLine();
        Console.Write("  Select attack: ");

        string input = Console.ReadLine();

        if (input == "0")
        {
            missionRunning = false;
            break;
        }

        // Seçilen saldırıya göre log üret
        EventLog log = null;

        if (input == "1")
            log = simulator.DoPortScan(targetName);
        else if (input == "2")
            log = simulator.DoBruteForce(targetName);
        else if (input == "3")
            log = simulator.DoDDoS(targetName);
        else if (input == "4")
            log = simulator.DoSqlInjection(targetName);
        else if (input == "5")
            log = simulator.DoFileAccess(targetName);
        else
        {
            Console.WriteLine("  Invalid selection. Try again.");
            Console.ReadLine();
            continue;
        }

        // Kural motoru çalıştır
        log.RuleDetected = ruleEngine.Check(log);

        // ML modeli çalıştır
        float mlProb = 0;
        log.MLDetected = mlDetector.Predict(log, out mlProb);
        log.MLProbability = mlProb;

        // Risk skoru hesapla
        log.RiskScore = riskScorer.Calculate(log.RuleDetected, log.MLProbability);
        log.ThreatLevel = riskScorer.GetLevel(log.RiskScore);

        // İki katmandan biri tespit ettiyse detected sayılır
        bool detected = log.RuleDetected || log.MLDetected;

        // Confusion matrix için kaydet
        metrics.Add(detected, log.IsMalicious);

        // Olay sonucunu ekrana yaz
        Console.WriteLine();
        Console.WriteLine("  ----------------------------------------------------------------------");
        Console.WriteLine("  Time        : " + log.Timestamp.ToString("HH:mm:ss"));
        Console.WriteLine("  Source IP   : " + log.SourceIP);
        Console.WriteLine("  Attack Type : " + log.AttackType);
        Console.WriteLine("  Rule Engine : " + (log.RuleDetected ? "DETECTED" : "NOT DETECTED"));
        Console.WriteLine("  ML Model    : " + (log.MLDetected ? "DETECTED" : "NOT DETECTED") +
                          "  (Probability: " + log.MLProbability.ToString("F2") + ")");
        Console.WriteLine("  Risk Score  : " + log.RiskScore.ToString("F2"));
        Console.WriteLine("  Threat Level: " + log.ThreatLevel);
        Console.WriteLine("  Result      : " + (detected ? ">>> ATTACK DETECTED <<<" : "--- MISSED ---"));
        Console.WriteLine("  ----------------------------------------------------------------------");
        Console.WriteLine();
        Console.Write("  Press Enter to continue...");
        Console.ReadLine();
    }

    // Görev sonu özeti
    Console.Clear();
    Console.WriteLine("======================================================================");
    Console.WriteLine("  MISSION COMPLETE - " + targetName);
    Console.WriteLine("======================================================================");
    metrics.PrintAll();
    Console.WriteLine();
    Console.Write("  Start a new mission? (y/n): ");
    string again = Console.ReadLine();

    if (again != "y" && again != "Y")
        running = false;
}

Console.WriteLine();
Console.WriteLine("  CyberDuel session ended. Goodbye.");
Console.WriteLine();