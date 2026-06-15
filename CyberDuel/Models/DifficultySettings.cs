namespace CyberDuel.Models
{
    public class DifficultySettings
    {
        public string Name = "";
        public double WAFBlockRate = 0.65;       // WAF'ın payload engelleme ihtimali
        public int LockoutThreshold = 10;         // kaç başarısız denemede lockout
        public double CapacityMultiplier = 1.0;   // sunucu kapasitesi çarpanı
        public double PasswordMissingChance = 0.40; // şifrenin wordlist'te olmama ihtimali
        public bool IDSActive = true;             // IDS gerçek zamanlı müdahale eder mi
        public double InjectionSuccessRate = 0.55; // WAF bypass olmadan injection başarı ihtimali

        public static DifficultySettings Easy()
        {
            return new DifficultySettings
            {
                Name = "EASY",
                WAFBlockRate = 0.30,
                LockoutThreshold = 15,
                CapacityMultiplier = 1.30,
                PasswordMissingChance = 0.0,
                IDSActive = false,
                InjectionSuccessRate = 0.75
            };
        }

        public static DifficultySettings Medium()
        {
            return new DifficultySettings
            {
                Name = "MEDIUM",
                WAFBlockRate = 0.65,
                LockoutThreshold = 10,
                CapacityMultiplier = 1.0,
                PasswordMissingChance = 0.40,
                IDSActive = true,
                InjectionSuccessRate = 0.55
            };
        }

        public static DifficultySettings Hard()
        {
            return new DifficultySettings
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
}