using System;
using System.Threading.Tasks;

namespace Fishare.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client();
            client.Connect("localhost");
            client.ReceiveFiles();

            client.GetFileData();
            while (true){}
        }
    }
}
