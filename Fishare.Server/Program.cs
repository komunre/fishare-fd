using System;
using System.Threading;

namespace Fishare.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1) {
                if (args[1] == "debug") {
                    Debug.Debugging = true;
                }
            }
            Server server = new Server();
            server.Initialize(12999);
            server.Listen();
            while (true) {
                for (int i = 0; i < server.ClientsCounter; i++) {
                    server.AcceptFiles(i);
                }
            }
        }
    }
}
