namespace CyberDuel.Systems
{
    // İnternete açık web proxy sunucusu
    public class WebGateway
    {
        public string Name = "Public Web Gateway";
        public string IP = "10.0.0.1";
        public string Description = "Internet-facing proxy and load balancer";
        public int[] OpenPorts = { 80, 443, 8080 };

        public string GetMission()
        {
            return "MISSION: Take the Web Gateway offline.\n" +
                   "Goal: Disrupt service using a DDoS Flood attack.";
        }
    }
}