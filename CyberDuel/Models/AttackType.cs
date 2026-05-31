namespace CyberDuel.Models
{
    // Saldırı türlerini tutan enum
    public enum AttackType
    {
        Normal = 0,
        PortScan = 1,
        BruteForce = 2,
        DDoSFlood = 3,
        SqlInjection = 4,
        FileAccess = 5
    }
}