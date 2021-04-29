using System.Net;
using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Fishare.Server {
    public class Server {
        private Socket listener;
        private Dictionary<string, Socket> clients = new Dictionary<string, Socket>();
        public int ClientsCounter {
            get => clients.Count;
        }
        
        public void Initialize(int port) {
            IPHostEntry hostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress address = /*hostInfo.AddressList[0];*/ IPAddress.Any;
            IPEndPoint endPoint = new IPEndPoint(address, port);

            listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        
            listener.Bind(endPoint);
            listener.Listen(100);
            Console.WriteLine("Server started");
        }

        public async void Listen() {
            await Task.Run(() => {
                Console.WriteLine("Listening...");
                while (true) {
                    Socket client = listener.Accept();
                    string ident = FishareRandom.RandomString();
                    clients.Add(ident, client);
                    client.Send(Encoding.UTF8.GetBytes(ident));
                    Console.WriteLine("Client connected");
                }
            });
        }

        public async void AcceptFiles(int client) {
            await Task.Run(() => {
                byte[] file_info = new byte[114];
                if (clients.ElementAt(client).Value != null) {
                    try {
                        int received = clients.ElementAt(client).Value.Receive(file_info);
                    }
                    catch {
                        Console.WriteLine("Connection closed");
                        clients.ElementAt(client).Value.Close();
                    }
                }
                if (!clients.ElementAt(client).Value.Connected){
                    return;
                }
                if (clients.ElementAt(client).Value == null){
                    return;
                }
                byte[] file_size;
                if (!BitConverter.IsLittleEndian){
                    Console.WriteLine("Converting to big endian");
                    file_size = new byte[] {file_info[110], file_info[111], file_info[112], file_info[113]};
                }
                else {
                    Console.WriteLine("Converting to little endian");
                    file_size = new byte[] {file_info[113], file_info[112], file_info[111], file_info[110]};
                }
                byte[] fileData = new byte[BitConverter.ToInt32(file_size)];
                if (clients.ElementAt(client).Value != null) {
                    int received = clients.ElementAt(client).Value.Receive(fileData);
                }

                var receiver = file_info.Skip(25).Take(25);
                Socket receiverSock;
                List<byte> dataToSend = new List<byte>();
                //dataToSend.AddRange(Encoding.UTF8.GetBytes(clients.ElementAt(client).Key));
                dataToSend.AddRange(file_info.Skip(110).Take(4).ToArray());
                dataToSend.AddRange(file_info.Skip(50).Take(60));
                dataToSend.AddRange(fileData);
                if (clients.TryGetValue(Encoding.UTF8.GetString(receiver.ToArray()), out receiverSock)) {
                    receiverSock.Send(dataToSend.ToArray());
                    Console.WriteLine("File sended");
                }
                else {
                    Console.WriteLine("Error in getting receiver");
                }
            });
        }
    }
}