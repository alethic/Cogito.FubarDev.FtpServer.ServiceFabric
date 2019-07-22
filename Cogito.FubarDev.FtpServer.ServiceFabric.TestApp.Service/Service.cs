using System.Collections.Generic;
using System.Fabric;

using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.FileSystem.DotNet;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace Cogito.FubarDev.FtpServer.ServiceFabric.TestApp.Service
{

    public class Service : StatelessService
    {

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="context"></param>
        public Service(StatelessServiceContext context)
            : base(context)
        { }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                {
                    return new FtpCommunicationListener(serviceContext, "ServiceEndpoint", (host, port) =>
                    {
                        var services = new ServiceCollection();

                        return services
                            .Configure<FtpServerOptions>(opt => (opt.ServerAddress, opt.Port) = (host, port))
                            .AddFtpServer(builder =>
                                builder
                                    .EnableAnonymousAuthentication()
                                    .UseDotNetFileSystem())
                            .BuildServiceProvider()
                            .GetRequiredService<IFtpServer>();
                    });
                }, "ftpListener")
            };
        }
    }
}
