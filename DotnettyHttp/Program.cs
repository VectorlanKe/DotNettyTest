using DotNetty.Transport.Channels;
using System;

namespace DotnettyHttp
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpServer httpServer = HttpServer.InitializeCreate()
                                    .RunServerAsync(5003);
            Console.WriteLine($"Httpd started. Listening on {httpServer.bootstrapChannel.LocalAddress}");
            Console.ReadLine();
        }
    }
}
