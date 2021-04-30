using System.Net.Sockets;

namespace Fishare.Server {
    public class FiSocket {
        public bool Disposed = false;
        public ClientStatus Status;
        public Socket Socket;
        public FiSocket(Socket sock) {
            Socket = sock;
            Status = ClientStatus.FREE;
        }
    }
}