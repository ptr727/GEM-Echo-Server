using System;
using System.Globalization;
using System.Net;

namespace GEMEchoServer
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            int port = 8000;
            if (args.Length > 0)
                port = int.Parse(args[0], CultureInfo.InvariantCulture);
            string csvFile = null;
            if (args.Length > 1)
                csvFile = args[1];

            Console.WriteLine($"Listening on port {port}");
            if (!string.IsNullOrEmpty(csvFile))
                Console.WriteLine($"Writing output to \"{csvFile}\"");
            GemServer gemServer = new GemServer(IPAddress.Any, port, csvFile);
            gemServer.Start();

            Console.WriteLine("Press Enter to stop...");
            Console.ReadLine();

            gemServer.Stop();
            gemServer.Dispose();
        }
    }
}
