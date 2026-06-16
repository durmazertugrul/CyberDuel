namespace CyberDuel.Models
{
    public class DifficultySettings
    {
        public string Name = "";
        public double WAFBlockRate = 0.65;
        public int LockoutThreshold = 10;
        public double CapacityMultiplier = 1.0;
        public double PasswordMissingChance = 0.40;
        public bool IDSActive = true;
        public double InjectionSuccessRate = 0.55;

        public static DifficultySettings Easy() => new DifficultySettings
        {
            Name = "EASY",
            WAFBlockRate = 0.30,
            LockoutThreshold = 15,
            CapacityMultiplier = 1.30,
            PasswordMissingChance = 0.0,
            IDSActive = false,
            InjectionSuccessRate = 0.75
        };

        public static DifficultySettings Medium() => new DifficultySettings
        {
            Name = "MEDIUM",
            WAFBlockRate = 0.65,
            LockoutThreshold = 10,
            CapacityMultiplier = 1.0,
            PasswordMissingChance = 0.40,
            IDSActive = true,
            InjectionSuccessRate = 0.55
        };

        public static DifficultySettings Hard() => new DifficultySettings
        {
            Name = "HARD",
            WAFBlockRate = 0.85,
            LockoutThreshold = 5,
            CapacityMultiplier = 0.70,
            PasswordMissingChance = 0.60,
            IDSActive = true,
            InjectionSuccessRate = 0.30
        };
    }
}