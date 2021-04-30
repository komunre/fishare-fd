using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fishare.Client {
    public class Client {
        Socket sender;
        byte[] ident = new byte[25];
        
        public void Connect(string address) {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(address);  
            IPAddress ipAddress = ipHostInfo.AddressList[0];  
            IPEndPoint remoteEP = new IPEndPoint(ipAddress,12999);

            sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            sender.Connect(remoteEP);
            Console.WriteLine("Connected");
            sender.Receive(ident);
            Console.WriteLine(Encoding.UTF8.GetString(ident));
        }

        private void SendFile(string fileName, string receiver) {
            byte[] fileContent;
            try {
                fileContent = File.ReadAllBytes(fileName);
            }
            catch {
                Console.WriteLine("Error reading file");
                return;
            }
            List<byte> data = new List<byte>();
            data.AddRange(ident);
            data.AddRange(Encoding.UTF8.GetBytes(receiver));
            data.AddRange(Encoding.UTF8.GetBytes(fileName));
            data.AddRange(new byte[60 - fileName.Length]);
            UInt32 len = (uint)fileContent.Length;
            data.AddRange(/*BitConverter.GetBytes(fileContent.Length)*/ new byte[] { (byte)(len), (byte)(len >> 8), (byte)(len >> 16), (byte)(len >> 24)});
            Console.WriteLine("Sending " + data.Count + " bytes");

            sender.Send(data.ToArray());
            sender.Send(fileContent);
            Console.WriteLine("Sending {0} bytes file", fileContent.Length);
            Console.WriteLine("File sended");
        }

        private byte[] ReceiveAll(uint size, Socket sock) {
            int total = 0;
            byte[] data = new byte[size];
            while (total < size) {
                int getted = sock.Receive(data, total, (int)(size - total), SocketFlags.None);
                if (getted == 0) {
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

        public async void ReceiveFiles() {
            await Task.Run(() => {
                while (true){
                    byte[] info = new byte[64];
                    int receivedInfo = sender.Receive(info);
                    if (info.Length != receivedInfo) {
                        Console.WriteLine("Wrong file info received");
                        return;
                    }

                    UInt32 fileSize = BitConverter.ToUInt32(info.Take(4).ToArray());
                    Console.WriteLine("File size: " + fileSize);
                    byte[] fileData = ReceiveAll(fileSize, sender);

                    string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"/"+new Random().Next(10000000).ToString();
                    Console.WriteLine("Writing to " + path);
                    Console.WriteLine(fileData);
                    File.WriteAllBytes(path, fileData);
                    Console.WriteLine("File received");
                }
            });
        }

        public async void GetFileData() {
            await Task.Run(() => {
                while (true) {
                    Console.Write("Enter file name: ");
                    string file = Console.ReadLine();
                    Console.Write("Enter receiver identificator: ");
                    string ident = Console.ReadLine();
            
                    SendFile(file, ident);
                }
            });
        }
    }
}