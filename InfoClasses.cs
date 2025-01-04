using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Bridge
{
    /// <summary>
    /// Class representing user information.
    /// </summary>
    public class UserInfo
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public string Email { get; set; }
    }

    /// <summary>
    /// Class representing guild information.
    /// </summary>
    public class GuildInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
    }

    /// <summary>
    /// Class representing connection information.
    /// </summary>
    public class ConnectionInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
