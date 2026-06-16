namespace CyberDuel.Scoring
{
    public class MissionScorer
    {
        private int score = 0, attackCount = 0, successCount = 0, detectedCount = 0;
        public int Score => score;

        public void RecordAttack(bool attackSuccess, bool detected)
        {
            attackCount++;
            if (attackSuccess) { successCount++; score += 100; if (!detected) score += 50; }
            if (detected) { detectedCount++; score -= 25; }
            if (score < 0) score = 0;
        }

        public void AddServerDownBonus() { score += 200; }
        public void AddAdminAccessBonus() { score += 150; }
        public void AddDatabaseBreachBonus() { score += 175; }

        public string GetRating()
        {
            if (score >= 700) return "ELITE";
            if (score >= 450) return "ADVANCED";
            if (score >= 200) return "INTERMEDIATE";
            return "NOVICE";
        }

        public void PrintSummary()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  [ MISSION SCORE ]");
            Console.WriteLine("  Total Attacks : " + attackCount);
            Console.WriteLine("  Successful    : " + successCount);
            Console.WriteLine("  Detected      : " + detectedCount);
            Console.WriteLine("  Final Score   : " + score + " pts");
            Console.WriteLine("  Rating        : " + GetRating());
            Console.ResetColor();
        }

        public void Reset() { score = 0; attackCount = 0; successCount = 0; detectedCount = 0; }
    }
}