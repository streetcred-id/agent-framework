﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Decorators.Transport;
using AgentFramework.Core.Exceptions;
using AgentFramework.Core.Extensions;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentFramework.Core.Handlers
{
    /// <summary>
    /// Base agent implementation
    /// </summary>
    public abstract class AgentMessageProcessorBase
    {
        private readonly IList<IMessageHandler> _handlers;

        /// <summary>Gets the provider.</summary>
        /// <value>The provider.</value>
        protected IServiceProvider Provider { get; }

        /// <summary>Gets the connection service.</summary>
        /// <value>The connection service.</value>
        protected IConnectionService ConnectionService { get; }

        /// <summary>Gets the message service.</summary>
        /// <value>The message service.</value>
        protected IMessageService MessageService { get; }

        /// <summary>Gets the logger.</summary>
        /// <value>The logger.</value>
        protected ILogger<AgentMessageProcessorBase> Logger { get; }

        /// <summary>Initializes a new instance of the <see cref="AgentMessageProcessorBase"/> class.</summary>
        protected AgentMessageProcessorBase(IServiceProvider provider)
        {
            Provider = provider;
            ConnectionService = provider.GetRequiredService<IConnectionService>();
            MessageService = provider.GetRequiredService<IMessageService>();
            Logger = provider.GetRequiredService<ILogger<AgentMessageProcessorBase>>();
            _handlers = new List<IMessageHandler>();
        }

        /// <summary>Adds a handler for supporting default connection flow.</summary>
        protected void AddConnectionHandler() => _handlers.Add(Provider.GetRequiredService<DefaultConnectionHandler>());

        /// <summary>Adds a handler for supporting default credential flow.</summary>
        protected void AddCredentialHandler() => _handlers.Add(Provider.GetRequiredService<DefaultCredentialHandler>());

        /// <summary>Adds the handler for supporting default proof flow.</summary>
        protected void AddTrustPingHandler() => _handlers.Add(Provider.GetRequiredService<DefaultTrustPingMessageHandler>());

        /// <summary>Adds the handler for supporting default proof flow.</summary>
        protected void AddProofHandler() => _handlers.Add(Provider.GetRequiredService<DefaultProofHandler>());

        /// <summary>Adds a default forwarding handler.</summary>
        protected void AddForwardHandler() => _handlers.Add(Provider.GetRequiredService<DefaultForwardHandler>());

        /// <summary>Adds a default forwarding handler.</summary>
        protected void AddEphemeralChallengeHandler() => _handlers.Add(Provider.GetRequiredService<DefaultEphemeralChallengeHandler>());

        /// <summary>Adds a default forwarding handler.</summary>
        protected void AddDiscoveryHandler() => _handlers.Add(Provider.GetRequiredService<DefaultDiscoveryHandler>());

        /// <summary>Adds a custom the handler using dependency injection.</summary>
        /// <typeparam name="T"></typeparam>
        protected void AddHandler<T>() where T : IMessageHandler => _handlers.Add(Provider.GetRequiredService<T>());

        /// <summary>Adds an instance of a custom handler.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">The instance.</param>
        protected void AddHandler<T>(T instance) where T : IMessageHandler => _handlers.Add(instance);

        /// <summary>
        /// Invoke the handler pipeline and process the passed message.
        /// </summary>
        /// <param name="context">The agent context.</param>
        /// <param name="messageContext">The message context.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Expected inner message to be of type 'ForwardMessage'</exception>
        /// <exception cref="AgentFrameworkException">Couldn't locate a message handler for type {messageType}</exception>
        /// TODO should recieve a message context and return a message context.
        protected async Task<byte[]> ProcessAsync(IAgentContext context, IMessageContext messageContext)
        {
            EnsureConfigured();

            context.SupportedMessages = GetSupportedMessageTypes();

            var agentContext = context.ToHandlerAgentContext();
            agentContext.AddNext(messageContext);

            MessageContext outgoingMessageContext = null;
            while (agentContext.TryGetNext(out var message) && outgoingMessageContext == null)
            {
                outgoingMessageContext = await ProcessMessage(agentContext, message);
            }

            return outgoingMessageContext?.Payload;
        }

        private async Task<MessageContext> ProcessMessage(IAgentContext agentContext, IMessageContext inboundMessageContext)
        {
            if (inboundMessageContext.Packed)
            {
                inboundMessageContext = await UnpackAsync(agentContext, inboundMessageContext);
                Logger.LogInformation($"Agent Message Received : {inboundMessageContext.ToJson()}");
            }

            if (_handlers.Where(handler => handler != null).FirstOrDefault(
                    handler => handler.SupportedMessageTypes.Any(
                        type => type.Equals(inboundMessageContext.GetMessageType(), StringComparison.OrdinalIgnoreCase))) is
                IMessageHandler messageHandler)
            {
                Logger.LogDebug("Processing message type {MessageType}, {MessageData}", 
                    inboundMessageContext.GetMessageType(),
                    inboundMessageContext.Payload.GetUTF8String());

                var response = await messageHandler.ProcessAsync(agentContext, inboundMessageContext);

                if (response != null)
                {
                    if (inboundMessageContext.ReturnRoutingRequested())
                    {
                        var result = await MessageService.PrepareForConnectionAsync(agentContext.Wallet, response, inboundMessageContext.Connection, null, false);
                        return new MessageContext(result, true, inboundMessageContext.Connection);
                    }
                    else
                    {
                        await MessageService.SendToConnectionAsync(agentContext.Wallet, response, inboundMessageContext.Connection);
                    }
                }
                return null;
            }

            throw new AgentFrameworkException(ErrorCode.InvalidMessage,
                $"Couldn't locate a message handler for type {inboundMessageContext.GetMessageType()}");
        }

        private async Task<IMessageContext> UnpackAsync(IAgentContext agentContext, IMessageContext message)
        {
            UnpackResult unpacked;

            try
            {
                unpacked = await CryptoUtils.UnpackAsync(agentContext.Wallet, message.Payload);
            }
            catch(Exception e)
            {
                Logger.LogError("Failed to un-pack message", e);
                throw new AgentFrameworkException(ErrorCode.InvalidMessage, "Failed to un-pack message", e);
            }

            if (unpacked.SenderVerkey != null && message.Connection == null)
            {
                try
                {
                    var connection = await ConnectionService.ResolveByMyKeyAsync(agentContext, unpacked.RecipientVerkey);
                    message = new MessageContext(unpacked.Message, false, connection);
                }
                catch (AgentFrameworkException ex) when (ex.ErrorCode == ErrorCode.RecordNotFound)
                {
                    // OK if not resolved. Example: authpacked forward message in routing agent.
                    // Downstream consumers should throw if Connection is required
                }
            }
            else
            {
                message = new MessageContext(unpacked.Message, false, message.Connection);
            }

            return message;
        }

        private IList<IMessageType> GetSupportedMessageTypes() => _handlers.SelectMany(_ => _.SupportedMessageTypes, (parent, child) => new MessageType(child) as IMessageType).ToList();

        private void EnsureConfigured()
        {
            if (_handlers == null || !_handlers.Any())
                ConfigureHandlers();
        }

        /// <summary>Configures the handlers.</summary>
        protected virtual void ConfigureHandlers()
        {
            AddConnectionHandler();
            AddForwardHandler();
        }
    }
}