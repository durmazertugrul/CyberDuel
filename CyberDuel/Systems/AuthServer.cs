namespace CyberDuel.Systems
{
    // Kullanıcı kimlik doğrulama sunucusu
    public class AuthServer
    {
        public string Name = "Authentication Server";
        public string IP = "192.168.10.10";
        public string Description = "Manages user credentials and access tokens";
        public int[] OpenPorts = { 22, 389, 636 };

        public string GetMission()
        {
            return "MISSION: Launch a brute force attack on the Authentication Server.\n" +
                   "Goal: Gain access to the admin account.";
        }
    }
}