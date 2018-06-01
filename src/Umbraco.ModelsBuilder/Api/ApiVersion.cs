using System;
using System.Reflection;
using Semver;

namespace Umbraco.ModelsBuilder.Api
{
    /// <summary>
    /// Manages API version handshake between client and server.
    /// </summary>
    public class ApiVersion
    {
        #region Configure

        // indicate the minimum version of the client API that is supported by this server's API.
        //   eg our Version = 4.8 but we support connections from VSIX down to version 3.2
        //   => as a server, we accept connections from client down to version ...
        private static readonly SemVersion MinClientVersionSupportedByServerConst = SemVersion.Parse("8.0.0-alpha.19");

        // indicate the minimum version of the server that can support the client API
        //   eg our Version = 4.8 and we know we're compatible with website server down to version 3.2
        //   => as a client, we tell the server down to version ... that it should accept us
        private static readonly SemVersion MinServerVersionSupportingClientConst = SemVersion.Parse("8.0.0-alpha.19");

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiVersion"/> class.
        /// </summary>
        /// <param name="executingVersion">The currently executing version.</param>
        /// <param name="minClientVersionSupportedByServer">The min client version supported by the server.</param>
        /// <param name="minServerVersionSupportingClient">An opt min server version supporting the client.</param>
        /// <exception cref="ArgumentNullException"></exception>
        internal ApiVersion(SemVersion executingVersion, SemVersion minClientVersionSupportedByServer, SemVersion minServerVersionSupportingClient = null)
        {
            Version = executingVersion ?? throw new ArgumentNullException(nameof(executingVersion));
            MinClientVersionSupportedByServer = minClientVersionSupportedByServer ?? throw new ArgumentNullException(nameof(minClientVersionSupportedByServer));
            MinServerVersionSupportingClient = minServerVersionSupportingClient;
        }

        private static SemVersion CurrentAssemblyVersion 
            => SemVersion.Parse(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

        /// <summary>
        /// Gets the currently executing API version.
        /// </summary>
        public static ApiVersion Current { get; }
            = new ApiVersion(CurrentAssemblyVersion, MinClientVersionSupportedByServerConst, MinServerVersionSupportingClientConst);

        /// <summary>
        /// Gets the executing version of the API.
        /// </summary>
        public SemVersion Version { get; }

        /// <summary>
        /// Gets the min client version supported by the server.
        /// </summary>
        public SemVersion MinClientVersionSupportedByServer { get; }

        /// <summary>
        /// Gets the min server version supporting the client.
        /// </summary>
        public SemVersion MinServerVersionSupportingClient { get; }

        /// <summary>
        /// Gets a value indicating whether the API server is compatible with a client.
        /// </summary>
        /// <param name="clientVersion">The client version.</param>
        /// <param name="minServerVersionSupportingClient">An opt min server version supporting the client.</param>
        /// <remarks>
        /// <para>A client is compatible with a server if the client version is greater-or-equal _minClientVersionSupportedByServer
        /// (ie client can be older than server, up to a point) AND the client version is lower-or-equal the server version
        /// (ie client cannot be more recent than server) UNLESS the server .</para>
        /// </remarks>
        public bool IsCompatibleWith(SemVersion clientVersion, SemVersion minServerVersionSupportingClient = null)
        {
            // client cannot be older than server's min supported version
            if (clientVersion < MinClientVersionSupportedByServer)
                return false;

            // if we know about this client (client is older than server), it is supported
            if (clientVersion <= Version) // if we know about this client (client older than server)
                return true;

            // if we don't know about this client (client is newer than server),
            // give server a chance to tell client it is, indeed, ok to support it
            return minServerVersionSupportingClient != null && minServerVersionSupportingClient <= Version;
        }
    }
}
