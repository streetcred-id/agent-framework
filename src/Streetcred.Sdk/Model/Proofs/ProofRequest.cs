﻿using Newtonsoft.Json;

namespace Streetcred.Sdk.Model.Proofs
{
    /// <summary>
    /// A proof request content message.
    /// </summary>
    public class ProofRequest : IContentMessage
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("@type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        public string Content { get; set; }
    }
}
