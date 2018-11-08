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
        {

        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            yield return new ServiceInstanceListener(serviceContext =>
                new FtpCommunicationListener(serviceContext, (host, port) =>
                    CreateFtpServer(host, port)));
        }

        IFtpServer CreateFtpServer(string host, int port)
        {
            var c = new ServiceCollection();
            c.Configure<FtpServerOptions>(o => { o.ServerAddress = host; o.Port = port; });
            c.Configure<DotNetFileSystemOptions>(o => o.RootPath = Context.CodePackageActivationContext.WorkDirectory);
            c.AddScoped<IFileSystemClassFactory, DotNetFileSystemProvider>();
            c.AddFtpServer(o => o.EnableAnonymousAuthentication().UseDotNetFileSystem());
            return c.BuildServiceProvider().GetService<IFtpServer>();
        }

    }

}
