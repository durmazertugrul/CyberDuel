namespace CyberDuel.Systems
{
    public class AuthServer
    {
        public string Name = "Authentication Server";
        public string IP = "192.168.10.10";
        public string Description = "Manages user credentials and access tokens";

        public Dictionary<int, (string Service, string Status)> PortTable = new()
        {
            { 22,   ("SSH",        "open")     },
            { 389,  ("LDAP",       "open")     },
            { 636,  ("LDAPS",      "open")     },
            { 8080, ("HTTP",       "open")     },
            { 443,  ("HTTPS",      "open")     },
            { 80,   ("HTTP",       "closed")   },
            { 3389, ("RDP",        "closed")   },
            { 5432, ("PostgreSQL", "filtered") },
            { 88,   ("Kerberos",   "open")     },
            { 9090, ("Web-UI",     "filtered") }
        };

        public bool HasWAF = false;
        public int MaxCapacity = 500;

        public Dictionary<string, string> FileSystem = new()
        {
            { "/public/status.html",     "guest" },
            { "/auth/tokens/",           "user"  },
            { "/var/log/auth.log",       "user"  },
            { "/config/ldap.conf",       "admin" },
            { "/config/kerberos.keytab", "admin" },
            { "/etc/passwd",             "root"  },
            { "/etc/shadow",             "root"  },
            { "/root/.ssh/id_rsa",       "root"  }
        };

        public Dictionary<string, string> UserAccounts = new()
        {
            { "guest",    "guest"      },
            { "ldapuser", "ldap123"    },
            { "sysop",    "sysop2024"  },
            { "admin",    "S3cur3Auth!"}
        };

        public string GetMission()
        {
            return "MISSION: Compromise the Authentication Server.\n" +
                   "Goal: Gain admin access via brute force login.";
        }
    }
}