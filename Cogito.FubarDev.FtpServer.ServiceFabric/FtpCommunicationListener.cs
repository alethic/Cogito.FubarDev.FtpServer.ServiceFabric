using System;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer;

using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace Cogito.FubarDev.FtpServer.ServiceFabric
{

    public class FtpCommunicationListener : ICommunicationListener
    {

        readonly ServiceContext serviceContext;
        readonly string endpointName;
        readonly Func<string, int, IFtpServer> build;

        IFtpServer ftpServer;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="endpointName"></param>
        /// <param name="build"></param>
        public FtpCommunicationListener(
            ServiceContext serviceContext,
            string endpointName,
            Func<string, int, IFtpServer> build)
        {
            this.serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
            this.endpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            this.build = build ?? throw new ArgumentNullException(nameof(build));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="build"></param>
        public FtpCommunicationListener(
            ServiceContext serviceContext,
            Func<string, int, IFtpServer> build) :
            this(serviceContext, "ServiceEndpoint", build)
        {

        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            return StartFtpServer(cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return StopFtpServer(cancellationToken);
        }

        public void Abort()
        {
            StopFtpServer(CancellationToken.None).Wait();
        }

        /// <summary>
        /// Starts the FTP server. Returns the address the service is listening on.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<string> StartFtpServer(CancellationToken cancellationToken)
        {
            var endpoint = serviceContext.CodePackageActivationContext.GetEndpoint(endpointName);
            if (endpoint == null || endpoint.Protocol != EndpointProtocol.Tcp)
                throw new InvalidOperationException($"Unable to find TCP endpoint named '{endpointName}'.");

            ftpServer = build(serviceContext.NodeContext.IPAddressOrFQDN, endpoint.Port);
            if (ftpServer == null)
                throw new InvalidOperationException("Unable to build FtpServer instance.");

            try
            {
                // queues the FTP server to start
                ftpServer.Start();

                // ftp server starts in background, wait for completion
                while (ftpServer.Ready == false && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                // yield out address server ended up listening on
                return new Uri($"ftp://{ftpServer.ServerAddress}:{ftpServer.Port}/").ToString();
            }
            catch (Exception)
            {
                await StopFtpServer(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Stops the running FTP server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task StopFtpServer(CancellationToken cancellationToken)
        {
            if (ftpServer != null)
            {
                try
                {
                    await Task.Run(() => ftpServer.Stop());
                }
                catch (Exception)
                {
                    try
                    {
                        if (ftpServer != null)
                            ftpServer.Stop();
                    }
                    catch (Exception)
                    {
                        // gave it our best go.
                    }
                }
                finally
                {
                    ftpServer = null;
                }
            }
        }

    }

}