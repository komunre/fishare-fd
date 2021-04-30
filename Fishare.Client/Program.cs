using System;
using System.Threading.Tasks;

namespace Fishare.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client();
            client.Connect();
            client.ReceiveFiles();

            client.GetFileData();
            while (true){}
        }
    }
}
