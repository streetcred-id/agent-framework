﻿using System.Threading.Tasks;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Models.Records;
using Hyperledger.Indy.WalletApi;

namespace AgentFramework.Core.Contracts
{
    /// <summary>
    /// Router service.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Prepares a wire level message from the application level agent message asynchronously.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message context.</param>
        /// <param name="recipientKey">The key to encrypt the message for.</param>
        /// <param name="routingKeys">The routing keys to pack the message for.</param>
        /// <param name="senderKey">The sender key to encrypt the message from.</param>
        /// <returns>The response async.</returns>
        Task<byte[]> PrepareAsync(Wallet wallet, AgentMessage message, string recipientKey, string[] routingKeys = null,
            string senderKey = null);

        /// <summary>
        /// Sends the agent message asynchronously.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="connection">The connection record.</param>
        /// <param name="recipientKey">The recipients verkey to encrypt the message for.</param>
        /// <returns>The response async.</returns>
        Task SendToConnectionAsync(Wallet wallet, AgentMessage message, ConnectionRecord connection, string recipientKey = null);

        /// <summary>
        /// Sends the agent message to the endpoint asynchronously.
        /// </summary>
        /// <param name="wallet">The wallet.</param>
        /// <param name="message">The message.</param>
        /// <param name="recipientKey">The recipients key.</param>
        /// <param name="endpointUri">The destination endpoint.</param>
        /// <param name="routingKeys">The routing keys.</param>
        /// <param name="senderKey">The senders key.</param>
        /// <returns>The response async.</returns>
        Task SendToEndpoint(Wallet wallet, AgentMessage message, string recipientKey,
            string endpointUri, string[] routingKeys = null, string senderKey = null);
    }
}
