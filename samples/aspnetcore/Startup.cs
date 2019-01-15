using System;
using System.IO;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.AspNetCore.Middleware;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Handlers.Default;
using Jdenticon.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAgent.Messages;
using WebAgent.Utils;

namespace WebAgent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            // Register agent framework dependency services and handlers
            services.AddAgent(c => c.SetPoolOptions(new PoolOptions { GenesisFilename = Path.GetFullPath("pool_genesis.txn") }),
                              s => s.OverrideCoreMessageHandlers()
                                    .AddMessageHandler<PrivateMessageHandler>()
                                    .AddMessageHandler<DefaultConnectionHandler>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            // Add agent middleware
            var agentBaseUrl = new Uri(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
            app.UseAgent<AgentMiddleware>($"{new Uri(agentBaseUrl, "/agent")}",
                obj =>
                {
                    obj.AddOwnershipInfo(NameGenerator.GetRandomName(), null);
                });

            // fun identicons
            app.UseJdenticon();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
