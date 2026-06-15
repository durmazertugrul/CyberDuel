# CyberDuel
### Console-Based AI Cybersecurity Simulation

A console application developed in C# that simulates realistic cyber attack scenarios against virtual target systems and evaluates defensive detection performance using both rule-based logic and a machine learning model.

> Developed as a final project for the **Computer and Network Security** course  
> Muğla Sıtkı Koçman University — Faculty of Technology  
> Instructor: Assoc. Prof. Dr. Enis Karaarslan

---

## Overview

CyberDuel places the user in the role of an attacker targeting one of three simulated server environments. Each attack type runs a realistic step-by-step simulation — including wordlist-based brute force, live port scanning with service detection, cumulative DDoS load tracking, real SQL payload injection against WAF-protected targets, and role-based file access attempts with privilege escalation. Every action generates a structured event log that is analyzed in real time by two detection layers. At the end of each mission, the system reports standard IDS evaluation metrics.

---

## Features

- **3 target systems:** Finance Server, Authentication Server, Public Web Gateway — each with unique port tables, file systems, user accounts, WAF configuration and server capacity
- **5 realistic attack types:** Port Scan, Brute Force, DDoS Flood, SQL Injection, Unauthorized File Access
- **Scenario-based missions** with objectives for each target
- **Synthetic log generation** — 600 labeled training samples with 9 features produced at startup
- **Rule-based detection engine** — multi-condition rules per attack type
- **ML-based detection** — FastTree binary classifier trained via ML.NET (9 features)
- **Risk scoring system** — combined score mapped to Low / Moderate / High / Critical
- **Performance metrics** — Precision, Recall, F1-Score, Confusion Matrix

---

## Project Structure

```
CyberDuel/
├── Models/
│   ├── AttackType.cs              # Enum: attack categories
│   ├── ThreatLevel.cs             # Enum: risk levels
│   ├── EventLog.cs                # Core event record (9 ML features)
│   └── MLEventData.cs             # ML.NET input/output types
├── Systems/
│   ├── FinanceServer.cs           # Target 1: port table, file system, WAF, accounts
│   ├── AuthServer.cs              # Target 2: port table, file system, accounts
│   └── WebGateway.cs              # Target 3: port table, file system, capacity
├── Attacks/
│   └── AttackSimulator.cs         # Realistic step-by-step attack simulations
├── Data/
│   └── SyntheticDataGenerator.cs  # Training data generator (9 features)
├── Detection/
│   ├── RuleEngine.cs              # Multi-condition rule-based detection
│   └── MLDetector.cs              # ML.NET FastTree classifier (9 features)
├── Scoring/
│   └── RiskScorer.cs              # Risk score + threat level
├── Evaluation/
│   └── MetricsReporter.cs         # Precision, Recall, F1, Confusion Matrix
└── Program.cs                     # Main console loop
```

---

## Requirements

| Component | Version |
|-----------|---------|
| .NET | 10.0 |
| ML.NET | Latest stable |
| ML.NET FastTree | Latest stable |
| IDE | Visual Studio 2026 |

---

## Installation

**1. Clone the repository:**
```bash
git clone https://github.com/durmazertugrul/CyberDuel.git
cd CyberDuel
```

**2. Install NuGet packages** (Package Manager Console):
```
Install-Package Microsoft.ML
Install-Package Microsoft.ML.FastTree
```

**3. Build and run:**
```bash
dotnet run
```

Or press **F5** in Visual Studio.

---

## How It Works

### Startup
When the program starts, `SyntheticDataGenerator` produces 600 labeled event records (300 normal, 300 attack across 5 types) and saves them to `training_data.csv`. The `MLDetector` trains a FastTree binary classifier on 80% of this data using 9 feature columns and reports accuracy on the remaining 20%.

### Simulation Loop
```
Select Target → Select Attack → Execute Realistic Simulation → Generate Log
                                                                    ↓
                                                      Rule Engine: multi-condition check
                                                      ML Model: 9-feature prediction
                                                                    ↓
                                                      Risk Score = 0.5×Rule + 0.5×ML
                                                      Threat Level assigned
                                                      Metrics accumulated
                                                                    ↓
                                                   [0] End Mission → Print Summary
```

### Attack Simulations

| Attack Type | What Happens |
|-------------|-------------|
| Port Scan | Probes each port in the target's port table, reports open/closed/filtered with service names |
| Brute Force | Iterates a 25-entry wordlist against real user accounts, triggers account lockout after 10 fails |
| DDoS Flood | Accumulates request rate each second against server capacity, tracks STABLE → DEGRADED → CRITICAL → DOWN |
| SQL Injection | Tests 12 real payloads against target, WAF blocks ~65% when active, reports table accessed on success |
| File Access | Attempts role-based access to each file in target's file system, optionally tries privilege escalation |

### Detection Rules

| Attack Type | Rule Conditions |
|-------------|----------------|
| Port Scan | Port count > 15 OR open ports found >= 3 |
| Brute Force | Attempts > 5 OR account lockout triggered |
| DDoS Flood | Request rate > 500 req/s OR rate > 350 AND duration > 3s |
| SQL Injection | Pattern flag = 1 OR WAF bypassed OR attempts > 3 |
| File Access | Restricted access flag = 1 OR attempts > 5 with restricted flag |

### ML Feature Vector (9 features)

| Feature | Description |
|---------|-------------|
| AttemptCount | Number of login or payload attempts |
| RequestRate | Requests per second (DDoS) |
| PortCount | Total ports probed |
| PatternFlag | Malicious SQL pattern matched (0/1) |
| RestrictedAccess | Restricted path accessed (0/1) |
| OpenPortsFound | Number of open ports discovered |
| LockoutTriggered | Account lockout activated (0/1) |
| WAFBypassed | WAF successfully bypassed (0/1) |
| AttackDuration | Total attack duration in seconds |

### Risk Scoring

```
RiskScore = 0.5 × RuleResult + 0.5 × MLProbability
```

| Score Range | Threat Level |
|-------------|-------------|
| 0.00 – 0.24 | Low |
| 0.25 – 0.49 | Moderate |
| 0.50 – 0.74 | High |
| 0.75 – 1.00 | Critical |

---

## Sample Output

```
  ══════════════════════════════════════════════════════════
  TARGET : Finance Server
  MISSION: Breach the Finance Server.
  Goal: Access sensitive data via SQL Injection or unauthorized file access.
  ──────────────────────────────────────────────────────────
  Round: 2   Events: 4   Precision: 0.86   Recall: 0.79   F1: 0.82
  ══════════════════════════════════════════════════════════

  [*] Initiating SQL Injection Attack...
  [*] Target: Finance Server
  [*] WAF Detected: YES — Evasion required
  [*] Loaded 12 injection payloads

  [PAYLOAD  1] ' OR '1'='1                          → WAF: BLOCKED
  [PAYLOAD  2] ' OR '1'='1' --                      → WAF: BLOCKED
  [PAYLOAD  3] admin' --                             → WAF: BLOCKED
  [PAYLOAD  7] ' UNION SELECT null, username...     → INJECTED ✓
  [+] Database access obtained → Table: customers

  ──────────────────────────────────────────────────────────
  [ DETECTION RESULT ]
  Rule Engine : FLAGGED
  ML Model    : FLAGGED   (Probability: 0.94)
  Risk Score  : 0.97
  Threat Level: Critical

  >>> ATTACK DETECTED — IDS ALERT RAISED <<<
  ──────────────────────────────────────────────────────────

  [ CONFUSION MATRIX ]
  TP (Correctly Detected) : 12
  TN (Correctly Ignored)  :  5
  FP (False Alarm)        :  2
  FN (Missed Attack)      :  1

  [ IDS PERFORMANCE METRICS ]
  Precision  : 0.86
  Recall     : 0.92
  F1-Score   : 0.89
  Total Events: 20
```

---

## Using Generative Artificial Intelligence

Some parts of this project were developed with the help of **Claude (Anthropic)**. Specifically:

- LINQ query templates in the data generator
- Project documentation and report writing
- Academic reference search and analysis of relevant studies

All architectural decisions, algorithm selection, detection logic, code writing, and final integration were done by the student.

---

## Academic References

1. Dina, A. S., Siddique, A. B., & Manivannan, D. (2022). Effect of balancing data using synthetic data on the performance of machine learning classifiers for intrusion detection in computer networks. *IEEE Access*, 10. https://doi.org/10.1109/ACCESS.2022.3205337

2. Jelo, M., & Helebrandt, P. (2022). Gamification of cyber ranges in cybersecurity education. *IEEE ICETA 2022*. https://doi.org/10.1109/ICETA57911.2022.9974714

3. Azam, Z., Islam, M. M., & Huda, M. N. (2023). Comparative analysis of intrusion detection systems and machine learning-based model analysis through decision tree. *IEEE Access*, 11. https://doi.org/10.1109/ACCESS.2023.3296444

4. Hossain, M. A., et al. (2024). An automatic network intrusion detection system using random forest on UNSW-NB15 dataset. *IEEE Access*, 12. https://doi.org/10.1109/ACCESS.2024.3368290

5. Al-Essa, M., et al. (2024). Advancing intrusion detection with machine learning: Insights from the UNSW-NB15 dataset. *IEEE ICICT 2024*. https://ieeexplore.ieee.org/document/10625148

---

## License

This project was developed for educational purposes as part of a university course assignment.
