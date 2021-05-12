using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TgSharp.Core.MTProto.Crypto;
using TcpClient = NetCoreServer.TcpClient;

namespace TgSharp.Core.Network
{
    public class TcpTransport : TcpClient, ITransport
    {
        
        // STATE Information to handle message framing
        private int packet_size = 0;
        private int packet_read = 0;

        private int packet_header_read = 0;
        private byte[] packet_header;
        private byte[] packet;
        private TaskCompletionSource<bool> connectCompletion;

        public TcpTransport(string address, int port) : base(address, port)
        {
        }

        public Task ConnectAsync()
        {
            connectCompletion = new TaskCompletionSource<bool>();
            base.ConnectAsync();
            return connectCompletion.Task;
        }

        public void Send(byte[] packet)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Client not connected to server.");

            var tcpMessage = new TcpMessage(sendCounter, packet).Encode();

            Debug.WriteLine("Sent data: " + BitConverter.ToString(tcpMessage));

            SendAsync(tcpMessage, 0, tcpMessage.Length);
            sendCounter++;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            Debug.WriteLine("Received data chunk: " + BitConverter.ToString(buffer, (int)offset, (int)size));

            while (offset < size)
            {
                if (packet_header_read != 4 && packet == null)
                {
                    if (packet_header == null) packet_header = new byte[4];
                    if (4 > packet_header_read && size > offset && packet_header != null)
                    {
                        int open = 4 - packet_header_read;

                        if (offset + open > size)
                        {
                            Buffer.BlockCopy(buffer, (int)offset, packet_header, packet_header_read, (int)(size - offset));

                            packet_header_read += (int)size - (int)offset;
                            offset = size;
                        }
                        else
                        {
                            Buffer.BlockCopy(buffer, (int)offset, packet_header, packet_header_read, open);

                            offset += open;

                            packet_size = BitConverter.ToInt32(packet_header, 0);

                            packet_read = 0;
                            packet = new byte[packet_size];
                        }
                    }
                }

                if (packet_size > packet_read && size > offset)
                {
                    var packet_open = packet_size - packet_read;

                    // Not full message in buffer
                    if (offset + packet_open > size)
                    {
                        // Copy rest to packet
                        Buffer.BlockCopy(buffer, (int)offset, packet, packet_read, (int)(size - offset));
                        // Array.Copy(buffer,offset,packet,packet_read,size-offset);
                        packet_read += (int)(size - offset);
                        offset = size;
                    }
                    // Buffer long enough
                    else
                    {
                        // Copy to the packet
                        Buffer.BlockCopy(buffer, (int)offset, packet, packet_read, packet_open);
                        // Array.Copy(buffer,offset,packet,packet_read,packet_open);
                        offset += packet_open;

                        // The total message has been received and will be processed within HandlePacket(byte[] data)... 
                        DecodePacket(packet_header, packet);

                        packet = null;
                        packet_size = 0;
                        packet_read = 0;
                        packet_header = null;
                        packet_header_read = 0;
                    }
                }
            }
        }

        private void DecodePacket(byte[] header, byte[] packet)
        {
            //TODO: maybe use TcpMessage.Decode to avoid duplication
            var tcpMessage =  new TcpMessage (-1, packet);

            var authKeyId = BitConverter.ToInt64(tcpMessage.Body, 0);

            if (authKeyId == 0)
                OnUnencryptedMessage?.Invoke(tcpMessage);
            else
                OnEncryptedMessage?.Invoke(tcpMessage);
        }

        protected override void OnConnected()
        {
            base.ReceiveAsync();
            base.Send(BitConverter.GetBytes(0xeeeeeeee));
            connectCompletion?.SetResult(true);
            connectCompletion = null;
            
        }

        protected override void OnError(SocketError error)
        {
            connectCompletion?.SetException(new Exception(error.ToString()));
            base.OnError(error);
        }

        protected override void OnDisconnected()
        {
            ReconnectAsync();
            base.OnDisconnected();

        }

        protected override void OnSent(long sent, long pending)
        {
            base.OnSent(sent, pending);
        }

        protected override void OnEmpty()
        {
            base.OnEmpty();
        }
    }
}
