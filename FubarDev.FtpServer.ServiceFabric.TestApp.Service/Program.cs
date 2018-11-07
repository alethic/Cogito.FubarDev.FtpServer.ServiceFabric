using System.Threading;

using Microsoft.ServiceFabric.Services.Runtime;

namespace FubarDev.FtpServer.ServiceFabric.TestApp.Service
{

    public static class Program
    {

        public static void Main()
        {
            ServiceRuntime.RegisterServiceAsync("FubarDev.FtpServer.ServiceFabric.TestApp.Service", context => new Service(context)).GetAwaiter().GetResult();
            Thread.Sleep(Timeout.Infinite);
        }

    }

}
