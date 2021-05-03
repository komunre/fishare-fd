using Fishare.Shared;

namespace Fishare.Client
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0) { 
                Debugger.LogLevel = int.Parse(args[0]);
                if (!Debugger.CheckLogLevel()) { 
                    Debugger.Log(0, "Error: Please enter valid log level (0 <= x <= 9)");
                    return;
                }
            }
            Client client = new Client();
            client.Connect();
            client.ReceiveFiles();

            client.GetFileData();
            while (true){}
        }
    }
}
