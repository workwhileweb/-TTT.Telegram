using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TgSharp.Core.Network
{
    public abstract class ITransport : IDisposable
    {
        public delegate void MessageHandler(TcpMessage message);
        public MessageHandler OnEncryptedMessage, OnUnencryptedMessage;

        public abstract Task ConnectAsync();

        public abstract void Send(byte[] packet);
        public abstract bool IsConnected();
        public abstract void Dispose();
    }
}
