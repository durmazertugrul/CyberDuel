namespace CyberDuel.Systems
{
    // Finansal verilerin tutulduğu sunucu
    public class FinanceServer
    {
        public string Name = "Finance Server";
        public string IP = "192.168.10.5";
        public string Description = "Stores high-value financial transactions and customer data";
        public int[] OpenPorts = { 443, 1433, 8443 };

        public string GetMission()
        {
            return "MISSION: Gain unauthorized access to the Finance Server.\n" +
                   "Goal: Reach sensitive data via SQL Injection or file access.";
        }
    }
}