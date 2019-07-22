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

        readonly ServiceContext _serviceContext;
        readonly string _endpointName;
        readonly Func<string, int, IFtpServer> _build;

        IFtpServer _ftpServer;

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
            var endpoint = _serviceContext.CodePackageActivationContext.GetEndpoint(_endpointName);
            if (endpoint == null || endpoint.Protocol != EndpointProtocol.Tcp)
                throw new InvalidOperationException($"Unable to find TCP endpoint named '{_endpointName}'.");

            _ftpServer = _build(_serviceContext.NodeContext.IPAddressOrFQDN, endpoint.Port);
            if (_ftpServer == null)
                throw new InvalidOperationException("Unable to build FtpServer instance.");

            try
            {
                // queues the FTP server to start
                _ftpServer.Start();

                // ftp server starts in background, wait for completion
                while (_ftpServer.Ready == false && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                // yield out address server ended up listening on
                return new Uri($"ftp://{_ftpServer.ServerAddress}:{_ftpServer.Port}/").ToString();
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
            if (_ftpServer != null)
            {
                try
                {
                    await Task.Run(() => _ftpServer.Stop());
                }
                catch (Exception)
                {
                    try
                    {
                        if (_ftpServer != null)
                            _ftpServer.Stop();
                    }
                    catch (Exception)
                    {
                        // gave it our best go.
                    }
                }
                finally
                {
                    _ftpServer = null;
                }
            }
        }

    }

}