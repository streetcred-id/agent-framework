﻿using AgentFramework.Core.Handlers;
using AgentFramework.Payments.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Payments.SovrinToken
{
    public static class AgentBuilderExtensions
    {
        public static AgentBuilder AddSovrinToken(this AgentBuilder agentBuilder)
        {
            agentBuilder.Services.AddHostedService<SovrinTokenConfigurationService>();
            agentBuilder.Services.AddSingleton<IPaymentService, SovrinPaymentService>();
            return agentBuilder;
        }
    }
}
