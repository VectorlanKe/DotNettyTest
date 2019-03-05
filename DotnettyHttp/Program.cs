using dotnet_etcd;
using DotNetty.Transport.Channels;
using Etcdserverpb;
using System;

namespace DotnettyHttp
{
    class Program
    {
        static void Main(string[] args)
        {
            DotnettyServer httpServer = DotnettyServer.InitializeCreate("127.0.0.1", 2379)
                                    .RunServerAsync(5003);
            Console.WriteLine($"Httpd started. Listening on {httpServer.BootstrapChannel.LocalAddress}");
            Console.ReadLine();
        }
    }
}
