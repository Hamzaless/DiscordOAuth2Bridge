using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Bridge
{
    /// <summary>
    /// Custom exception for OAuth errors
    /// </summary>
    public class OAuthException : Exception
    {
        public OAuthException(string message) : base(message) { }
    }
}
