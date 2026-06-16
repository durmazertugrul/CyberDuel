namespace CyberDuel.Systems
{
    public class FinanceServer
    {
        public string Name = "Finance Server";
        public string IP = "192.168.10.5";
        public string Description = "Stores high-value financial transactions and customer data";
        public Dictionary<int, (string Service, string Status)> PortTable = new()
        {
            { 80, ("HTTP","closed") }, { 443, ("HTTPS","open") }, { 1433, ("MSSQL","open") },
            { 22, ("SSH","filtered") }, { 3389, ("RDP","open") }, { 8080, ("HTTP-Alt","closed") },
            { 8443, ("HTTPS-Alt","open") }, { 21, ("FTP","closed") },
            { 25, ("SMTP","filtered") }, { 3306, ("MySQL","closed") }
        };
        public bool HasWAF = true;
        public int MaxCapacity = 800;
        public Dictionary<string, string> FileSystem = new()
        {
            { "/public/index.html","guest" }, { "/public/assets/logo.png","guest" },
            { "/app/dashboard.html","user" }, { "/var/log/transactions.log","user" },
            { "/admin/config.xml","admin" }, { "/admin/users.db","admin" },
            { "/db/customers.db","admin" }, { "/db/credit_cards.db","admin" },
            { "/etc/shadow","root" }, { "/etc/passwd","root" }
        };
        public Dictionary<string, string> UserAccounts = new()
        {
            { "guest","guest" }, { "analyst","analyst2024" },
            { "admin","Adm!n@Finance" }, { "dbadmin","D3faultPass!" }
        };
        public string GetMission() =>
            "MISSION: Breach the Finance Server.\n" +
            "Goal: Access sensitive data via SQL Injection or unauthorized file access.";
    }
}