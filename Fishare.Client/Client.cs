using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Fishare.Shared;

namespace Fishare.Client {
    public class Client {
        Socket sender;
        byte[] ident = new byte[25];
        
        public void Connect() {
            string addrp = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "config.txt"));
            string[] splitted = addrp.Split(":");
            IPHostEntry ipHostInfo = Dns.GetHostEntry(splitted[0]);  
            IPAddress ipAddress = ipHostInfo.AddressList[0];  
            if (ipHostInfo.AddressList.Length > 1) {
            	ipAddress = ipHostInfo.AddressList[1];
            }
            IPEndPoint remoteEP = new IPEndPoint(ipAddress,int.Parse(splitted[1]));
    
            sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            
            sender.Connect(remoteEP);
            Debugger.Log(0, "Connected");
            sender.Receive(ident);
            Debugger.Log(0, Encoding.UTF8.GetString(ident));
        }

        private void SendFile(string fileName, string receiver) {
            byte[] fileContent;
            try {
                fileContent = File.ReadAllBytes(fileName);
            }
            catch {
                Debugger.Log(0, "Error reading file");
                return;
            }
			
            List<byte> data = new List<byte>();
            data.AddRange(ident);
            data.AddRange(Encoding.UTF8.GetBytes(receiver));
            string[] fileNameSplitted;
            if (fileName.Contains('\\') && Environment.OSVersion.Platform == PlatformID.Win32NT) {
                fileNameSplitted = fileName.Split('\\');
            }
            else {
                fileNameSplitted = fileName.Split('/');
            }
            string fileNameLast = fileNameSplitted[fileNameSplitted.Length - 1];
            if(fileNameLast.Length > 60){
                Debugger.Log(0, "Too long filename");
                return;
            }
            data.AddRange(Encoding.UTF8.GetBytes(fileNameLast));
            data.AddRange(new byte[60 - fileNameLast.Length]);
            UInt32 len = (uint)fileContent.Length;
            data.AddRange(/*BitConverter.GetBytes(fileContent.Length)*/ new byte[] { (byte)(len), (byte)(len >> 8), (byte)(len >> 16), (byte)(len >> 24)});
            Debugger.Log(9, "Sending " + data.Count + " bytes");

            sender.Send(data.ToArray());
            Debugger.Log(3, String.Format("Sending {0} bytes file", fileContent.Length));
            sender.Send(fileContent);
            Debugger.Log(0, "File sended");
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
                Debugger.Log(1, String.Format("Getting... {0}%", (float)((float)total / (float)size) * 100));
            }
            Debugger.Log(9, String.Format("Total getted {0} bytes from stream", total));
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
                        Debugger.Log(0, "Wrong file info received");
                        return;
                    }

                    UInt32 fileSize = BitConverter.ToUInt32(info.Take(4).ToArray());
                    byte[] fileByteName = info.Skip(4).Take(60).ToArray();
                    int counter = 0;
                    for (byte fileCh = fileByteName[0]; fileCh != 0; counter++) {
                        fileCh = fileByteName[counter];
                    }
                    counter--;
                    fileByteName = fileByteName.Take(counter).ToArray();
                    string fileName = Encoding.UTF8.GetString(fileByteName);
                    Debugger.Log(2, "File size: " + fileSize);
                    byte[] fileData = ReceiveAll(fileSize, sender);

                    string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"/"+fileName;
                    Debugger.Log(0, "Writing to " + path);
                    File.WriteAllBytes(path, fileData);
                    Debugger.Log(0, "File received");
                }
            });
        }

        public async void GetFileData() {
            await Task.Run(() => {
                while (true) {
                    Debugger.Log(0, "Enter file name: ");
                    string file = Console.ReadLine();
                    Debugger.Log(0, "Enter receiver identificator: ");
                    string ident = Console.ReadLine();
            
                    SendFile(file, ident);
                }
            });
        }
    }
}
