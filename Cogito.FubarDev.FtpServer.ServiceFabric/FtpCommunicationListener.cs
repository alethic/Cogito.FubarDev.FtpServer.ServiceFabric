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
        private readonly ServiceContext _serviceContext;
        private readonly string _endpointName;
        private readonly Func<string, int, IFtpServer> _build;
        private IFtpServer _ftpServer;

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
            _serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
            _endpointName = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            _build = build ?? throw new ArgumentNullException(nameof(build));
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
        { }

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
            var endpoint = _serviceContext.CodePackageActivationContext.GetEndpoint(_endpointName);
            if (endpoint == null || endpoint.Protocol != EndpointProtocol.Tcp)
            {
                throw new InvalidOperationException($"Unable to find TCP endpoint named '{_endpointName}'.");
            }

            var host = _serviceContext.NodeContext.IPAddressOrFQDN;
            var port = endpoint.Port;

            _ftpServer = _build(host, port);

            if (_ftpServer == null)
            {
                throw new InvalidOperationException("Unable to build FtpServer instance.");
            }

            try
            {
                // queues the FTP server to start
                await _ftpServer.StartAsync(cancellationToken);

                // ftp server starts in background, wait for completion
                while (!_ftpServer.Ready && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }

                // yield out address server ended up listening on
                return new Uri($"ftp://{host}:{port}/").ToString();
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
            if (_ftpServer is IFtpServer)
            {
                try
                {
                    await _ftpServer.StopAsync(cancellationToken);
                }
                catch (Exception)
                {
                    _ftpServer = null;
                }
            }
        }

    }

}