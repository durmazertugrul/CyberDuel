namespace CyberDuel.Models
{
    // Görev boyunca biriken durum bilgisi — saldırılar arası state
    public class SessionState
    {
        // Port scan sonucunda bulunan açık portlar
        public List<int> DiscoveredOpenPorts = new List<int>();

        // DDoS ile sunucu çevrimdışı edildi mi
        public bool ServerOffline = false;

        // Brute force ile admin erişimi sağlandı mı
        public bool AdminAccessGained = false;
        public string CompromisedUsername = "";
        public string CompromisedPassword = "";

        // SQL injection ile veritabanına girildi mi
        public bool DatabaseBreached = false;
        public string BreachedTable = "";

        // Oturum boyunca gerçekleşen tüm eventler (log export için)
        public List<EventLog> AllEvents = new List<EventLog>();

        public void Reset()
        {
            DiscoveredOpenPorts.Clear();
            ServerOffline = false;
            AdminAccessGained = false;
            CompromisedUsername = "";
            CompromisedPassword = "";
            DatabaseBreached = false;
            BreachedTable = "";
            AllEvents.Clear();
        }
    }
}