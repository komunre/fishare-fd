using System.Net;
using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Fishare.Shared;

namespace Fishare.Server {

    public enum ClientStatus {
        BUSY,
        FREE,
    }
    public class Server {
        private Socket listener;
        private Dictionary<string, FiSocket> clients = new Dictionary<string, FiSocket>();
        private List<ClientStatus> clientStatuses = new List<ClientStatus>();
        private const string _dummySock = "unavailable";
        public int ClientsCounter {
            get => clients.Count;
        }
        
        public void Initialize(int port) {
            FiSocket dummy = new FiSocket(null);
            clients.Add(_dummySock, dummy); // Please replace this with real fix

            IPHostEntry hostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress address = /*hostInfo.AddressList[0];*/ IPAddress.Any;
            IPEndPoint endPoint = new IPEndPoint(address, port);

            listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        
            listener.Bind(endPoint);
            listener.Listen(100);
            Debugger.Log(0, "Server started");
        }

        public async void Listen() {
            await Task.Run(() => {
                Debugger.Log(0, "Listening...");
                while (true) {
                    Socket client = listener.Accept();
                    string ident = FishareRandom.RandomString();
                    clients.Add(ident, new FiSocket(client));
                    clientStatuses.Add(ClientStatus.FREE);
                    client.Send(Encoding.UTF8.GetBytes(ident));
                    Debugger.Log(0, "Client connected");
                }
            });
        }

        public void CloseConnection(string key) { 
            lock (clients) {
                ref Socket client = ref clients[key].Socket;
                client.Shutdown(SocketShutdown.Both);
                client.Disconnect(false);
                client.Dispose();
                Debugger.Log(0, "Connection closed");
            }
        }

        private byte[] ReceiveAll(uint size, string key) {
            ref Socket sock = ref clients[key].Socket;
            int total = 0;
            byte[] data = new byte[size];
            while (total < size) {
                int getted = sock.Receive(data, total, (int)(size - total), SocketFlags.None);
                if (getted == 0) {
                    CloseConnection(key);
                    data = null;
                    break;
                }
                total += getted;
            }
            Debugger.Log(2, String.Format("Total getted {0} bytes from stream", total));
            if (total == 0){
                return null;
            }
            return data;
        }
        public async void AcceptFiles(int client) {
            await Task.Run(() => {
                if (clients.Count < 1 || clients == null) {
                    return;
                }
                List<KeyValuePair<string, FiSocket>> sockets;
                lock(clients) { sockets = clients.ToList(); }
                if (sockets[client].Key == _dummySock || sockets[client].Value.Socket == null || sockets[client].Value == null || sockets[client].Value.Status == ClientStatus.BUSY) {
                    return;
                }
                sockets[client].Value.Status = ClientStatus.BUSY;
                byte[] file_info = new byte[114];
                if (!sockets[client].Value.Socket.Connected || sockets[client].Value == null) {
                    return;
                }
                if (sockets[client].Value != null) {
                    try {
                        int received = sockets[client].Value.Socket.Receive(file_info);
                        if (received == 0) {
                            CloseConnection(sockets[client].Key);
                        }
                    }
                    catch (SocketException){
                        CloseConnection(sockets[client].Key);
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
                Debugger.Log(3, String.Format("Receiving {0} bytes file", BitConverter.ToUInt32(file_size)));
                byte[] fileData = ReceiveAll(BitConverter.ToUInt32(file_size), sockets[client].Key);
                if (fileData == null) {
                    return;
                }

                Debugger.Log(0, "Sending file...");
                var receiver = file_info.Skip(25).Take(25);
                FiSocket receiverSock;
                List<byte> dataToSend = new List<byte>();
                //dataToSend.AddRange(Encoding.UTF8.GetBytes(clients.ElementAt(client).Key));
                Int32 len = fileData.Length;
                dataToSend.AddRange(new byte[] { (byte)len, (byte)(len >> 8), (byte)(len >> 16), (byte)(len >> 24)});
                dataToSend.AddRange(file_info.Skip(50).Take(60));
                dataToSend.AddRange(fileData);
                if (clients.Count != 0) {
                    sockets = clients.ToList();
                }
                if (Encoding.UTF8.GetString(receiver.ToArray()) != _dummySock && clients.TryGetValue(Encoding.UTF8.GetString(receiver.ToArray()), out receiverSock)) {
                    try {
                        receiverSock.Socket.Send(dataToSend.ToArray());
                    }
                    catch (SocketException) {
                        Debugger.Log(0, "Writing error");
                        CloseConnection(Encoding.UTF8.GetString(receiver.ToArray()));
                        return;
                    }
                    Debugger.Log(0, "File sended");
                }
                else {
                    Debugger.Log(0, "Error in getting receiver");
                }
                sockets[client].Value.Status = ClientStatus.FREE;
            });
        }
    }
}