using System.Net;
using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Fishare.Server {

    public enum ClientStatus {
        BUSY,
        FREE,
    }
    public class Server {
        private Socket listener;
        private Dictionary<string, Socket> clients = new Dictionary<string, Socket>();
        private Dictionary<string, ClientStatus> clientStatuses = new Dictionary<string, ClientStatus>();
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
                    clientStatuses.Add(ident, ClientStatus.FREE);
                    client.Send(Encoding.UTF8.GetBytes(ident));
                    Console.WriteLine("Client connected");
                }
            });
        }

        public void CloseConnection(Socket client) {   
            client.Shutdown(SocketShutdown.Both);
            client.Disconnect(false);
            client.Close();
            Console.WriteLine("Connection closed");
        }

        private byte[] ReceiveAll(uint size, Socket sock) {
            int total = 0;
            byte[] data = new byte[size];
            while (total < size) {
                int getted = sock.Receive(data, total, (int)(size - total), SocketFlags.None);
                if (getted == 0) {
                    CloseConnection(sock);
                    data = null;
                    break;
                }
                total += getted;
            }
            Console.WriteLine("Total getted {0} bytes from stream", total);
            if (total == 0){
                return null;
            }
            return data;
        }
        public async void AcceptFiles(int client) {
            await Task.Run(() => {
                if (clientStatuses.ElementAt(client).Value == ClientStatus.BUSY) {
                    return;
                }
                clientStatuses[clients.ElementAt(client).Key] = ClientStatus.BUSY;
                byte[] file_info = new byte[114];
                if (!clients.ElementAt(client).Value.Connected || clients.ElementAt(client).Value == null) {
                    return;
                }
                if (clients.ElementAt(client).Value != null) {
                    try {
                        int received = clients.ElementAt(client).Value.Receive(file_info);
                    }
                    catch (SocketException){
                        return;
                    }
                }
                byte[] file_size;
                /*if (!BitConverter.IsLittleEndian) {
                    Console.WriteLine("Converting to big endian");
                    file_size = new byte[] {file_info[110], file_info[111], file_info[112], file_info[113]};
                }
                else {
                    Console.WriteLine("Converting to little endian");
                    file_size = new byte[] {file_info[113], file_info[112], file_info[111], file_info[110]};
                }*/
                file_size = new byte[] {file_info[110], file_info[111], file_info[112], file_info[113]};
                Console.WriteLine(String.Format("Receiving {0} bytes file", BitConverter.ToUInt32(file_size)));
                byte[] fileData = ReceiveAll(BitConverter.ToUInt32(file_size), clients.ElementAt(client).Value);
                if (fileData == null) {
                    return;
                }

                Console.WriteLine("Sending file...");
                var receiver = file_info.Skip(25).Take(25);
                Socket receiverSock;
                List<byte> dataToSend = new List<byte>();
                //dataToSend.AddRange(Encoding.UTF8.GetBytes(clients.ElementAt(client).Key));
                Int32 len = fileData.Length;
                dataToSend.AddRange(new byte[] { (byte)len, (byte)(len >> 8), (byte)(len >> 16), (byte)(len >> 24)});
                dataToSend.AddRange(file_info.Skip(50).Take(60));
                dataToSend.AddRange(fileData);
                if (clients.TryGetValue(Encoding.UTF8.GetString(receiver.ToArray()), out receiverSock)) {
                    receiverSock.Send(dataToSend.ToArray());
                    Console.WriteLine("File sended");
                }
                else {
                    Console.WriteLine("Error in getting receiver");
                }
                clientStatuses[clients.ElementAt(client).Key] = ClientStatus.FREE;
            });
        }
    }
}