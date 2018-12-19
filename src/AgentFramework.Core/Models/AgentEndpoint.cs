﻿namespace AgentFramework.Core.Models
{
    /// <summary>
    /// An object for containing agent endpoint information.
    /// </summary>
    public class AgentEndpoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentEndpoint"/> class.
        /// </summary>
        /// <param name="copy">The copy.</param>
        internal AgentEndpoint(AgentEndpoint copy)
        {
            Did = copy.Did;
            Verkey = copy.Verkey;
            Uri = copy.Uri;
        }

        internal AgentEndpoint() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentEndpoint"/> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="did">The did.</param>
        /// <param name="verkey">The verkey.</param>
        public AgentEndpoint(string uri, string did, string verkey)
        {
            Uri = uri;
            Did = did;
            Verkey = verkey;
        }

        /// <summary>
        /// Gets or sets the did of the agent.
        /// </summary>
        /// <value>
        /// The did of the agent.
        /// </value>
        public string Did { get; internal set; }

        /// <summary>
        /// Gets or sets the verkey of the agent.
        /// </summary>
        /// <value>
        /// The verkey of the agent.
        /// </value>
        public string Verkey { get; internal set; }

        /// <summary>
        /// Gets or sets the uri of the agent.
        /// </summary>
        /// <value>
        /// The uri of the agent.
        /// </value>
        public string Uri { get; internal set; }
    }
}