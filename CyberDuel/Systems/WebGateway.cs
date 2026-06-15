namespace CyberDuel.Systems
{
    public class WebGateway
    {
        public string Name = "Public Web Gateway";
        public string IP = "10.0.0.1";
        public string Description = "Internet-facing proxy and load balancer";

        public Dictionary<int, (string Service, string Status)> PortTable = new()
        {
            { 80,   ("HTTP",       "open")     },
            { 443,  ("HTTPS",      "open")     },
            { 8080, ("HTTP-Alt",   "open")     },
            { 8443, ("HTTPS-Alt",  "open")     },
            { 22,   ("SSH",        "filtered") },
            { 3306, ("MySQL",      "closed")   },
            { 21,   ("FTP",        "closed")   },
            { 25,   ("SMTP",       "closed")   },
            { 9090, ("WebUI",      "open")     },
            { 6379, ("Redis",      "filtered") }
        };

        public bool HasWAF = false;
        public int MaxCapacity = 600;

        public Dictionary<string, string> FileSystem = new()
        {
            { "/public/index.html",       "guest" },
            { "/public/api/status",       "guest" },
            { "/config/nginx.conf",       "user"  },
            { "/var/log/access.log",      "user"  },
            { "/admin/proxy_rules.conf",  "admin" },
            { "/admin/ssl_certs/",        "admin" },
            { "/etc/shadow",              "root"  }
        };

        public Dictionary<string, string> UserAccounts = new()
        {
            { "guest",   "guest"       },
            { "monitor", "monitor123"  },
            { "admin",   "G@teway2024" }
        };

        public string GetMission()
        {
            return "MISSION: Take the Web Gateway offline.\n" +
                   "Goal: Overwhelm the server with a DDoS Flood attack.";
        }
    }
}