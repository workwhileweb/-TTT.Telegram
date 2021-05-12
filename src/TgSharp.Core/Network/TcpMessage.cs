using System;
using System.IO;
using System.Linq;
using TgSharp.Core.MTProto.Crypto;

namespace TgSharp.Core.Network
{
    public class TcpMessage
    {
        public byte[] Body { get; private set; }

        public TcpMessage(byte[] body)
        {
            if (body == null)
                throw new ArgumentNullException(nameof(body));

            Body = body;
        }

        public static TcpMessage Decode(byte[] body)
        {
            using (var memoryStream = new MemoryStream(body))
            {
                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    int length = binaryReader.ReadInt32();
                    return new TcpMessage(binaryReader.ReadBytes(length));
                }
            }
        }

        public byte[] Encode()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    // https://core.telegram.org/mtproto#tcp-transport
                    /*
                        4 length bytes are added at the front 
                        (to include the length, the sequence number, and CRC32; always divisible by 4)
                        and 4 bytes with the packet sequence number within this TCP connection 
                        (the first packet sent is numbered 0, the next one 1, etc.),
                        and 4 CRC32 bytes at the end (length, sequence number, and payload together).
                    */
                    binaryWriter.Write(Body.Length);
                    binaryWriter.Write(Body);
                    var transportPacket = memoryStream.ToArray();

                    //					Debug.WriteLine("Tcp packet #{0}\n{1}", SequneceNumber, BitConverter.ToString(transportPacket));

                    return transportPacket;
                }
            }
        }

    }
}
