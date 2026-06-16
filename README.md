# CyberDuel
### Console-Based AI Cybersecurity Simulation

A console application developed in C# that simulates realistic, step-by-step cyber attack scenarios against configurable virtual target systems and evaluates defensive detection performance through a hybrid rule-based and machine learning pipeline.

> Developed as a final project for the **Computer and Network Security** course  
> Muğla Sıtkı Koçman University — Faculty of Technology  
> Instructor: Assoc. Prof. Dr. Enis Karaarslan

---

## Overview

CyberDuel places the user in the role of an attacker targeting one of three simulated server environments. Each attack type executes a realistic simulation: brute force iterates a real wordlist against actual user accounts and triggers account lockout; port scan probes a defined port table port by port and stores discovered services for subsequent use; DDoS tracks cumulative request rate against server capacity and transitions through STABLE, DEGRADED, CRITICAL, and DOWN states; SQL injection tests a list of known payloads against WAF-protected targets with a success rate that scales with the number of payloads attempted; file access navigates a role-based permission system and optionally attempts privilege escalation.

A persistent session state carries information across attacks within a mission: discovered open ports enhance subsequent injection attempts, compromised credentials elevate file access privileges, and a server taken offline blocks all further attacks. When a target is compromised, lateral movement bonuses apply to the next target. The IDS responds in real time during attacks — throttling connections, activating rate limiting, and permanently banning IPs that exceed the detection threshold. Every event is analyzed by two independent detection layers and contributes to a running mission score.

---

## Features

**Simulation**
- 3 difficulty levels (Easy / Medium / Hard) controlling WAF strength, lockout threshold, server capacity, password availability, and IDS aggressiveness
- 3 target systems, each with a unique port table, role-based file system, user accounts, WAF flag, and server capacity
- 5 realistic attack types with step-by-step console output
- Stealth mode for each attack — slower execution, fewer payloads, reduced IDS trigger chance; SQL injection success rate scales proportionally with payload count
- Normal / Stealth mode selection before each attack

**Session Logic**
- Persistent session state across all attacks in a mission
- Attack chaining: port 22 discovery redirects brute force to SSH usernames; DB port discovery enhances SQL injection; successful brute force elevates file access starting role
- Server offline state persists — once taken down via DDoS, all subsequent attacks are blocked
- Lateral movement: compromising one target applies difficulty bonuses to the next
- Coordinated attack detection: 3+ different attack types within 90 seconds triggers an IDS alert
- Repeated pattern detection: same attack type 3 times consecutively triggers an IDS alert

**IDS and Detection**
- Real-time IDS intervention: connection throttling during brute force, rate limiting during DDoS, firewall adaptation during port scan
- IP tracking: repeat attackers detected with lower thresholds
- IP ban: 3 detections permanently ban the source IP for the session; banned-IP attacks are counted separately and excluded from rule/ML metrics
- Rule-based engine with multi-condition rules per attack type
- FastTree binary classifier (ML.NET) trained on 600 synthetic records with 9 features
- Synthetic data includes borderline normal traffic and evasive attack samples to ensure ML independence from the rule engine
- ML interpretability via single-sample perturbation: each feature is individually set to baseline and the resulting probability drop is measured to identify top contributors
- False negative explanation: when an attack is missed, the system explains which threshold was not reached

**Evaluation and Reporting**
- Per-attack-type Precision, Recall, F1-Score table
- ASCII bar chart of detection rates by attack type
- Separate Rule-Only / ML-Only / Hybrid layer comparison
- Pre-blocked IP counter shown separately from detection metrics
- SIEM-style event timeline at mission end
- Mission scoring with attack success bonuses, stealth bonuses, objective bonuses, and detection penalties
- Session rating (Novice / Intermediate / Advanced / Elite)
- JSON session log export with all feature values (enables replay analysis)
- Replay analysis: load a previous session log and re-run the current ML model to identify changed decisions
- Multi-session history table comparing score, F1, Rule F1, and ML F1 across missions

---

## Project Structure

```
CyberDuel/
├── Models/
│   ├── AttackType.cs              # Enum: attack categories
│   ├── ThreatLevel.cs             # Enum: risk levels
│   ├── EventLog.cs                # Core event record (9 ML features)
│   ├── MLEventData.cs             # ML.NET input/output types
│   ├── DifficultySettings.cs      # Easy / Medium / Hard configuration
│   ├── SessionState.cs            # Persistent state, layer metrics, pattern tracking
│   └── SessionRecord.cs           # Per-mission summary for history table
├── Systems/
│   ├── FinanceServer.cs           # Port table, file system, WAF, user accounts
│   ├── AuthServer.cs
│   └── WebGateway.cs
├── Attacks/
│   └── AttackSimulator.cs         # All 5 attack simulations with stealth mode
├── Data/
│   └── SyntheticDataGenerator.cs  # 600 records: normal, borderline, evasive, attack
├── Detection/
│   ├── RuleEngine.cs              # Multi-condition rule-based detection
│   ├── MLDetector.cs              # FastTree classifier + perturbation-based explanation
│   └── IPTracker.cs               # IP-level attack tracking and ban enforcement
├── Scoring/
│   ├── RiskScorer.cs              # Composite risk score and threat level mapping
│   └── MissionScorer.cs           # Point-based mission scoring
├── Evaluation/
│   ├── MetricsReporter.cs         # Confusion matrix, per-type metrics, bar chart
│   ├── SIEMTimeline.cs            # SIEM-style event timeline
│   └── ReplayAnalyzer.cs          # Re-analyze previous session logs with current ML model
└── Program.cs                     # Main loop: difficulty, state, detection, scoring, export
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
Select a difficulty level. `SyntheticDataGenerator` produces 600 labeled records — including borderline normal traffic and evasive attack samples — and saves them to `training_data.csv`. The `MLDetector` trains a FastTree binary classifier on 80% of this data using 9 features and reports accuracy and F1-score on the held-out 20%.

### Session Flow
```
Select Difficulty → Select Target → Mission Loop
                                         ↓
                       Select Attack + Mode (Normal / Stealth)
                                         ↓
                             Execute Realistic Simulation
                                         ↓
                         Check IP Ban → if banned: pre-block, skip pipeline
                                         ↓
                        Rule Engine (multi-condition) → boolean flag
                        ML Model (9-feature FastTree) → probability
                                         ↓
                         Risk Score = 0.5 × Rule + 0.5 × ML probability
                         Threat Level: Low / Moderate / High / Critical
                                         ↓
                         ML Explanation (perturbation-based)
                         Miss Explanation (if false negative)
                         Pattern Alert (coordinated / repeated)
                                         ↓
                         Metrics accumulated — Score recorded
                                         ↓
                     [0] End Mission → SIEM Timeline → Layer Comparison
                                    → IDS Metrics → Per-type Metrics
                                    → Bar Chart → Score → JSON Export
                                    → Replay option → New mission?
```

### Attack Chaining

| Discovery | Effect |
|-----------|--------|
| Port 22 open | Brute force targets SSH usernames |
| Port 389/636 open | Brute force targets LDAP usernames |
| Port 1433/3306 open | SQL injection success rate +20% |
| Brute force success | File access starts with admin role |
| DDoS success | Target goes offline — all further attacks blocked |
| Any target compromised | Next target receives lateral movement difficulty bonus |

### Difficulty Levels

| Setting | Easy | Medium | Hard |
|---------|------|--------|------|
| WAF Block Rate | 30% | 65% | 85% |
| Lockout Threshold | 15 fails | 10 fails | 5 fails |
| Server Capacity | +30% | Base | −30% |
| Password in Wordlist | Always | 60% chance | 40% chance |
| IDS Intervention | Off | Active | Active |

### Stealth Mode Effects

| Attack | Normal | Stealth |
|--------|--------|---------|
| Port Scan | All ports, 80ms delay | Random 60% subset, 160ms delay |
| Brute Force | Full wordlist | 8 attempts per username |
| DDoS | Step 80–200 req/s | Step 30–80 req/s |
| SQL Injection | 12 payloads, full rate | 6 payloads, rate × (6/12) |
| File Access | Full enumeration | First half of paths only |

### Detection Rules

| Attack Type | Conditions |
|-------------|-----------|
| Port Scan | Port count > 15 OR open ports ≥ 3 |
| Brute Force | Attempts > 5 OR lockout triggered |
| DDoS Flood | Rate > 500 req/s OR rate > 350 AND duration > 3s |
| SQL Injection | Pattern flag = 1 OR WAF bypassed OR attempts > 3 |
| File Access | Restricted access flag = 1 OR attempts > 5 with restricted flag |

### ML Feature Vector (9 features)

| Feature | Description |
|---------|-------------|
| AttemptCount | Login or payload attempt count |
| RequestRate | Requests per second |
| PortCount | Total ports probed |
| PatternFlag | Malicious SQL pattern matched (0/1) |
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
| Detected by IDS | −25 |

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
