﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Extensions.Options;
using Streetcred.Sdk.Model;
using Streetcred.Sdk.Model.Connections;
using Streetcred.Sdk.Model.Credentials;
using Streetcred.Sdk.Model.Proofs;

namespace Streetcred.Sdk.Extensions
{
    public class AgentMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDefaultWalletService _walletService;
        private readonly IDefaultPoolService _poolService;
        private readonly IDefaultMessageSerializer _messageSerializer;
        private readonly IDefaultConnectionService _connectionService;
        private readonly IDefaultCredentialService _credentialService;
        private readonly IDefaultProvisioningService _provisioningService;
        private readonly PoolOptions _poolOptions;
        private readonly WalletOptions _walletOptions;

        public AgentMiddleware(RequestDelegate next,
            IDefaultWalletService walletService,
            IDefaultPoolService poolService,
            IDefaultMessageSerializer messageSerializer,
            IDefaultConnectionService connectionService,
            IDefaultCredentialService credentialService,
            IDefaultProvisioningService provisioningService,
            IOptions<WalletOptions> walletOptions,
            IOptions<PoolOptions> poolOptions)
        {
            _next = next;
            _walletService = walletService;
            _poolService = poolService;
            _messageSerializer = messageSerializer;
            _connectionService = connectionService;
            _credentialService = credentialService;
            _provisioningService = provisioningService;
            _poolOptions = poolOptions.Value;
            _walletOptions = walletOptions.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var pool = await _poolService.GetPoolAsync(_poolOptions.PoolName, _poolOptions.ProtocolVersion);
            var wallet = await _walletService.GetWalletAsync(_walletOptions.WalletConfiguration,
                _walletOptions.WalletCredentials);
            var endpoint = await _provisioningService.GetProvisioningAsync(wallet);

            if (context.Request.ContentLength != null)
            {
                var body = new byte[(int) context.Request.ContentLength];

                await context.Request.Body.ReadAsync(body, 0, body.Length);

                var decrypted =
                    await _messageSerializer.UnpackAsync<IEnvelopeMessage>(body, wallet, endpoint.Endpoint.Verkey);
                var decoded = JsonConvert.DeserializeObject<IContentMessage>(decrypted.Content);

                switch (decoded)
                {
                    case ConnectionRequest request:
                        await _connectionService.ProcessRequestAsync(wallet, request);
                        break;
                    case ConnectionResponse response:
                        await _connectionService.ProcessResponseAsync(wallet, response);
                        break;
                    case CredentialOffer offer:
                        await _credentialService.ProcessOfferAsync(wallet, offer);
                        break;
                    case CredentialRequest request:
                        await _credentialService.ProcessCredentialRequestAsync(wallet, request);
                        break;
                    case Credential credential:
                        await _credentialService.ProcessCredentialAsync(pool, wallet, credential);
                        break;
                    case Proof _:
                        break;
                }

                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(string.Empty);
                return;
            }

            throw new Exception("Empty content length");
        }
    }
}