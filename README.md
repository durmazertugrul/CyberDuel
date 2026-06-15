# CyberDuel
### Console-Based AI Cybersecurity Simulation

A console application developed in C# that simulates realistic cyber attack scenarios against virtual target systems and evaluates defensive detection performance using both rule-based logic and a machine learning model.

> Developed as a final project for the **Computer and Network Security** course  
> Muğla Sıtkı Koçman University — Faculty of Technology  
> Instructor: Assoc. Prof. Dr. Enis Karaarslan

---

## Overview

CyberDuel places the user in the role of an attacker targeting one of three simulated server environments across three difficulty levels. Attacks are realistic and step-by-step: brute force iterates a real wordlist and triggers account lockout, port scan probes a defined port table and stores discovered ports for use in subsequent attacks, DDoS tracks cumulative load against server capacity until the target goes offline, SQL injection tests real payloads against WAF-protected targets, and file access navigates a role-based permission system with privilege escalation attempts. A session state persists across attacks — a compromised server stays offline, admin credentials elevate file access privileges, and discovered open ports improve injection success rates. Every event is analyzed by a hybrid two-layer detection system and contributes to a mission score.

---

## Features

- **3 difficulty levels:** Easy, Medium, Hard — affect WAF strength, lockout threshold, server capacity, and password availability
- **3 target systems:** Finance Server, Authentication Server, Public Web Gateway — each with port tables, file systems, user accounts, WAF flag, and server capacity
- **5 realistic attack types:** Port Scan, Brute Force, DDoS Flood, SQL Injection, Unauthorized File Access
- **Session state & attack chaining** — attack results persist and influence subsequent attacks
- **IDS real-time intervention** — throttling during brute force, rate limiting during DDoS, firewall adaptation during port scan
- **Synthetic log generation** — 600 labeled records with 9 features including borderline and evasive samples for ML independence
- **Rule-based detection** — multi-condition rules per attack type
- **ML-based detection** — FastTree binary classifier (ML.NET) trained on 9 features
- **Risk scoring** — combined score mapped to Low / Moderate / High / Critical
- **Mission scoring** — points for successful attacks with stealth and objective bonuses
- **JSON session log export** — full event log saved after each mission
- **Performance metrics** — Precision, Recall, F1-Score, Confusion Matrix

---

## Project Structure

```
CyberDuel/
├── Models/
│   ├── AttackType.cs              # Enum: attack categories
│   ├── ThreatLevel.cs             # Enum: risk levels
│   ├── EventLog.cs                # Core event record (9 ML features)
│   ├── MLEventData.cs             # ML.NET input/output types
│   ├── SessionState.cs            # Persistent state across attacks
│   └── DifficultySettings.cs      # Easy / Medium / Hard configuration
├── Systems/
│   ├── FinanceServer.cs           # Target 1: port table, file system, WAF, accounts
│   ├── AuthServer.cs              # Target 2: port table, file system, accounts
│   └── WebGateway.cs              # Target 3: port table, file system, capacity
├── Attacks/
│   └── AttackSimulator.cs         # Realistic step-by-step simulations with chaining
├── Data/
│   └── SyntheticDataGenerator.cs  # 600 records: normal, borderline, evasive, attack
├── Detection/
│   ├── RuleEngine.cs              # Multi-condition rule-based detection
│   └── MLDetector.cs              # ML.NET FastTree classifier (9 features)
├── Scoring/
│   ├── RiskScorer.cs              # Risk score + threat level
│   └── MissionScorer.cs           # Point-based mission scoring
├── Evaluation/
│   └── MetricsReporter.cs         # Precision, Recall, F1, Confusion Matrix
└── Program.cs                     # Main loop: difficulty, state, scoring, log export
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
Select a difficulty level. `SyntheticDataGenerator` produces 600 labeled records — including borderline normal traffic and evasive attack samples that fall below rule thresholds — and saves them to `training_data.csv`. The `MLDetector` trains a FastTree classifier on 80% of this data using 9 features.

### Session Flow
```
Select Difficulty → Select Target → Mission Loop
                                         ↓
                              Select Attack → Execute Simulation
                                         ↓
                              Session State Updated (ports, credentials, server status)
                                         ↓
                              Rule Engine + ML Model → Risk Score → Threat Level
                                         ↓
                              Score Recorded → Metrics Accumulated
                                         ↓
                              [0] End Mission → Summary + JSON Export
```

### Attack Chaining

| Previous Attack | Effect on Next Attack |
|-----------------|----------------------|
| Port Scan (port 1433/3306 found) | SQL Injection success rate +15% |
| Brute Force (success) | File Access starts with admin role |
| DDoS (server down) | All subsequent attacks blocked |

### Difficulty Levels

| Setting | Easy | Medium | Hard |
|---------|------|--------|------|
| WAF Block Rate | 30% | 65% | 85% |
| Lockout Threshold | 15 fails | 10 fails | 5 fails |
| Server Capacity | +30% | Base | -30% |
| Password in Wordlist | Always | 60% chance | 40% chance |
| IDS Intervention | Off | Active | Active |

### Detection Rules

| Attack Type | Rule Conditions |
|-------------|----------------|
| Port Scan | Port count > 15 OR open ports >= 3 |
| Brute Force | Attempts > 5 OR lockout triggered |
| DDoS Flood | Rate > 500 req/s OR rate > 350 AND duration > 3s |
| SQL Injection | Pattern flag = 1 OR WAF bypassed OR attempts > 3 |
| File Access | Restricted flag = 1 OR attempts > 5 with restricted flag |

### ML Feature Vector (9 features)

| Feature | Description |
|---------|-------------|
| AttemptCount | Login or payload attempt count |
| RequestRate | Requests per second |
| PortCount | Total ports probed |
| PatternFlag | SQL pattern matched (0/1) |
| RestrictedAccess | Restricted path accessed (0/1) |
| OpenPortsFound | Open ports discovered |
| LockoutTriggered | Account lockout activated (0/1) |
| WAFBypassed | WAF bypassed (0/1) |
| AttackDuration | Attack duration in seconds |

### Risk Scoring

```
RiskScore = 0.5 × RuleResult + 0.5 × MLProbability
```

| Score | Threat Level |
|-------|-------------|
| < 0.25 | Low |
| 0.25 – 0.49 | Moderate |
| 0.50 – 0.74 | High |
| ≥ 0.75 | Critical |

### Mission Scoring

| Event | Points |
|-------|--------|
| Successful attack | +100 |
| Undetected success (stealth bonus) | +50 |
| Server taken offline | +200 |
| Admin credentials obtained | +150 |
| Database breached | +175 |
| Detected by IDS | -25 |

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
