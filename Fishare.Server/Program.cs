using System;
using System.Threading;

namespace Fishare.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1) {
                Console.WriteLine("Please enter port");
                return;
            }
            Server server = new Server();
            server.Initialize(int.Parse(args[0]));
            server.Listen();
            while (true) {
                for (int i = 0; i < server.ClientsCounter; i++) {
                    server.AcceptFiles(i);
                }
            }
        }
    }
}
