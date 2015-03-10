using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    public class Compatibility
    {
        /// <summary>
        /// Gets ModelsBuilder version.
        /// </summary>
        public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;

        // indicate which versions of the client API are supported by this server's API.
        // (eg our Version = 4.8 but we support connections from VSIX down to version 3.2)
        public static readonly Version MinClientVersionSupportedByServer = new Version(2, 1, 0, 0);
        
        // indicate which versions of the server API support this client
        // (eg our Version = 4.8 and we know we're compatible with website server down to version 3.2)
        public static readonly Version MinServerVersionSupportingClient = new Version(2, 1, 0, 0);

        // say, version 42 adds an optional parameter to the API
        //  min client = 40 (because we support clients that do not send the optional parameter)
        //  min server for client 42 = 42 (because we're sending the optional parameter)
        // say, version 43 makes that parameter mandatory
        //  min client = 42 (because we don't support clients that do not send the now-mandatory parameter)
        //  min server for client 43 = 42 (because it sends the now-mandatory parameter)

        public static bool IsCompatible(Version clientVersion)
        {
            if (clientVersion <= Version) // if we know about this client (client older than server)
                return clientVersion >= MinClientVersionSupportedByServer; // check it is supported (we know)

            // cannot happen, newer clients should use the other API?!
            return false;
        }

        public static bool IsCompatible(Version clientVersion, Version minServerVersionSupportingClient)
        {
            return clientVersion <= Version // if we know about this client (client older than server)
                ? clientVersion >= MinClientVersionSupportedByServer // check it is supported (we know)
                : minServerVersionSupportingClient <= Version; // else do what client says
        }
    }
}
