using System.Net.Sockets;

namespace Fishare.Server {
    public class FiSocket : Socket {
        public bool Disposed;
        public FiSocket(AddressFamily family, SocketType type, ProtocolType ptype) : base(family, type, ptype) {
            Disposed = false;
        }

        public new void Close(){
            base.Close();
            Disposed = true;
        }
    }
}