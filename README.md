# CyberDuel
### Console-Based AI Cybersecurity Simulation

A console application developed in C# that simulates cyber attack scenarios against virtual target systems and evaluates defensive detection performance using both rule-based logic and a machine learning model.

> Developed as a final project for the **Computer and Network Security** course  
> Muğla Sıtkı Koçman University — Faculty of Technology  
> Instructor: Assoc. Prof. Dr. Enis Karaarslan

---

## Overview

CyberDuel places the user in the role of an attacker targeting one of three simulated server environments. Every action generates a synthetic event log that is immediately analyzed by two detection layers. At the end of each mission, the system reports standard IDS evaluation metrics.

The project aims to demonstrate attacker-defender dynamics in an accessible, educational setting without requiring real network infrastructure.

---

## Features

- **3 target systems:** Finance Server, Authentication Server, Public Web Gateway
- **5 attack types:** Port Scan, Brute Force, DDoS Flood, SQL Injection, Unauthorized File Access
- **Scenario-based missions** with objectives for each target
- **Synthetic log generation** — 500 labeled training samples produced at startup
- **Rule-based detection engine** — threshold and pattern matching rules
- **ML-based detection** — Decision Tree classifier trained via ML.NET (FastTree)
- **Risk scoring system** — combined score mapped to Low / Moderate / High / Critical
- **Performance metrics** — Precision, Recall, F1-Score, Confusion Matrix

---

## Project Structure

```
CyberDuel/
├── Models/
│   ├── AttackType.cs          # Enum: attack categories
│   ├── ThreatLevel.cs         # Enum: risk levels
│   ├── EventLog.cs            # Core event record
│   └── MLEventData.cs         # ML.NET input/output types
├── Systems/
│   ├── FinanceServer.cs       # Target system 1
│   ├── AuthServer.cs          # Target system 2
│   └── WebGateway.cs          # Target system 3
├── Attacks/
│   └── AttackSimulator.cs     # All 5 attack simulations
├── Data/
│   └── SyntheticDataGenerator.cs  # Training data generator
├── Detection/
│   ├── RuleEngine.cs          # Rule-based detection
│   └── MLDetector.cs          # ML.NET Decision Tree classifier
├── Scoring/
│   └── RiskScorer.cs          # Risk score + threat level
├── Evaluation/
│   └── MetricsReporter.cs     # Precision, Recall, F1, Confusion Matrix
└── Program.cs                 # Main console loop
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
git clone https://github.com/USERNAME/CyberDuel.git
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
When the program starts, `SyntheticDataGenerator` produces 500 labeled event records (250 normal, 250 attack across 5 types) and saves them to `training_data.csv`. The `MLDetector` then trains a FastTree binary classifier on 80% of this data and reports accuracy on the remaining 20%.

### Simulation Loop
```
Select Target → Select Attack → Execute → Generate Log
                                              ↓
                                    Rule Engine checks thresholds
                                    ML Model predicts probability
                                              ↓
                                    Risk Score = 0.5×Rule + 0.5×ML
                                    Threat Level assigned
                                    Metrics accumulated
                                              ↓
                                 [0] End Mission → Print Summary
```

### Detection Rules

| Attack Type | Rule Condition |
|-------------|---------------|
| Port Scan | Port count > 15 |
| Brute Force | Failed attempts > 5 |
| DDoS Flood | Request rate > 500 req/s |
| SQL Injection | Malicious pattern flag = 1 |
| File Access | Restricted path flag = 1 |

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
======================================================================
  TARGET : Finance Server
  MISSION: Gain unauthorized access to the Finance Server.
  Goal: Reach sensitive data via SQL Injection or file access.
----------------------------------------------------------------------
  Round: 3  |  Total Events: 7
  Precision: 0.86  |  Recall: 0.79  |  F1: 0.82
======================================================================

  [ ATTACK PANEL ]
  [1] Port Scan          - Fast multi-port probing
  [2] Brute Force        - Repeated login attempts
  [3] DDoS Flood         - High-volume traffic burst
  [4] SQL Injection      - Malicious query injection
  [5] File Access        - Unauthorized file access
  [0] End Mission

  ----------------------------------------------------------------------
  Time        : 14:02:26
  Source IP   : 172.16.0.8
  Attack Type : DDoSFlood
  Rule Engine : DETECTED
  ML Model    : DETECTED  (Probability: 0.97)
  Risk Score  : 0.74
  Threat Level: High
  Result      : >>> ATTACK DETECTED <<<
  ----------------------------------------------------------------------

  [ CONFUSION MATRIX ]
  TP (Correctly Detected) :    12
  TN (Correctly Ignored)  :     5
  FP (False Alarm)        :     2
  FN (Missed Attack)      :     1

  [ IDS PERFORMANCE METRICS ]
  Precision  : 0.86
  Recall     : 0.92
  F1-Score   : 0.89
  Total Events: 20
```

## Using Generative Artificial Intelligence

Some parts of this project were developed with the help of **Claude (Anthropic)**. Specifically:

- Initial class framework for model and system classes
- LINQ query templates in the data generator
- Project documentation and report writing
- Academic reference search and analysis of relevant studies

All architectural decisions, algorithm selection, detection logic, code writing, and final integration were done by the student.

## Academic References

1. Dina, A. S., Siddique, A. B., & Manivannan, D. (2022). Effect of balancing data using synthetic data on the performance of machine learning classifiers for intrusion detection in computer networks. *IEEE Access*, 10. https://doi.org/10.1109/ACCESS.2022.3205337

2. Jelo, M., & Helebrandt, P. (2022). Gamification of cyber ranges in cybersecurity education. *IEEE ICETA 2022*. https://doi.org/10.1109/ICETA57911.2022.9974714

3. Azam, Z., Islam, M. M., & Huda, M. N. (2023). Comparative analysis of intrusion detection systems and machine learning-based model analysis through decision tree. *IEEE Access*, 11. https://doi.org/10.1109/ACCESS.2023.3296444

4. Hossain, M. A., et al. (2024). An automatic network intrusion detection system using random forest on UNSW-NB15 dataset. *IEEE Access*, 12. https://doi.org/10.1109/ACCESS.2024.3368290

5. Al-Essa, M., et al. (2024). Advancing intrusion detection with machine learning: Insights from the UNSW-NB15 dataset. *IEEE ICICT 2024*. https://ieeexplore.ieee.org/document/10625148
6. 
---

## License

This project was developed for educational purposes as part of a university course assignment.
