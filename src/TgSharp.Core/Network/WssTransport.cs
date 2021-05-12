using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TgSharp.Core.MTProto.Crypto;
using Websocket.Client;

namespace TgSharp.Core.Network
{
    public class WssTransport : ITransport
    {
        private readonly WebsocketClient client;
        private const uint protocol = 0xeeeeeeee;
        private readonly RandomNumberGenerator rngSource;
        private readonly short datacenterId;

        private byte[] encryptKey, encryptIV, decryptKey, decryptIV;
        private readonly IBufferedCipher cipher;

        private byte[] encryptCount = new byte[16], decryptCount = new byte[16];
        private int encryptNum = 0, decryptNum = 0;

        public WssTransport(string address, int port, int dcId)
        {
            cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");

            Array.Clear(encryptCount, 0, encryptCount.Length);
            Array.Clear(decryptCount, 0, decryptCount.Length);

            var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
            {
                Options =
                    {
                        KeepAliveInterval = TimeSpan.FromMinutes(30)
                    }
            });

            rngSource = RandomNumberGenerator.Create();

            client = new WebsocketClient(new Uri($"ws://{address}:{port}/apiws"));
            client.MessageReceived.Subscribe(async msg => await HandleIncomingMessage(msg));

            datacenterId = (short)dcId;
        }

        private async Task HandleIncomingMessage(ResponseMessage msg)
        {
            Debug.WriteLine("Received data chunk: " + BitConverter.ToString(msg.Binary));
            byte[] unencryptedData = new byte[msg.Binary.Length];
            AesCtr.Ctr128Encrypt(msg.Binary, decryptKey, ref decryptIV, ref decryptCount, ref decryptNum, unencryptedData);

            var tcpMessage = TcpMessage.Decode(unencryptedData);

            var authKeyId = BitConverter.ToInt64(tcpMessage.Body, 0);

            if (authKeyId == 0)
                OnUnencryptedMessage?.Invoke(tcpMessage);
            else
                OnEncryptedMessage?.Invoke(tcpMessage);
        }

        public override async Task ConnectAsync()
        {
            await client.StartOrFail();
            //Client.Send(BitConverter.GetBytes(0xeeeeeeee));
            byte[] init = new byte[64];

            while (true)
            {
                rngSource.GetNonZeroBytes(init);
                if (init[0] == 0xef) //Conflict with abridged protocol identifier
                    continue;

                uint firstInt = BitConverter.ToUInt32(init, 0);
                if (firstInt == 0x44414548 || firstInt == 0x54534f50 || firstInt == 0x20544547 || firstInt == 0x4954504f || firstInt == 0x02010316 || firstInt == 0xdddddddd || firstInt == 0xeeeeeeee)
                    continue;

                uint secondInt = BitConverter.ToUInt32(init, 4);
                if (secondInt == 0x00000000)
                    continue;

                break;
            }

            Buffer.BlockCopy(BitConverter.GetBytes(protocol), 0, init, 56, 4);
            //Buffer.BlockCopy(BitConverter.GetBytes(datacenterId), 0, init, 60, 2);

            byte[] initRev = init.Reverse().ToArray();

            encryptKey = new byte[32];
            Buffer.BlockCopy(init, 8, encryptKey, 0, 32);
            decryptKey = new byte[32];
            Buffer.BlockCopy(initRev, 8, decryptKey, 0, 32);

            encryptIV = new byte[16];
            Buffer.BlockCopy(init, 40, encryptIV, 0, 16);
            decryptIV = new byte[16];
            Buffer.BlockCopy(initRev, 40, decryptIV, 0, 16);


            byte[] encryptedInit = new byte[init.Length];
            AesCtr.Ctr128Encrypt(init, encryptKey, ref encryptIV, ref encryptCount, ref encryptNum, encryptedInit);

            byte[] finalInit = new byte[64];
            Buffer.BlockCopy(init, 0, finalInit, 0, 56);
            Buffer.BlockCopy(encryptedInit, 56, finalInit, 56, 8);

            await client.SendInstant(finalInit);
        }

        public override void Send(byte[] packet)
        {

            if (!client.IsRunning)
                throw new InvalidOperationException("Client not connected to server.");

            var tcpMessage = new TcpMessage(packet).Encode();

            Debug.WriteLine("Sent data: " + BitConverter.ToString(tcpMessage));

            byte[] encryptedMessage = new byte[tcpMessage.Length];
            AesCtr.Ctr128Encrypt(tcpMessage, encryptKey, ref encryptIV, ref encryptCount, ref encryptNum, encryptedMessage);

            client.Send(encryptedMessage);
        }

        public override void Dispose()
        {
            client.Dispose();
        }

        public override bool IsConnected()
        {
            return client.IsRunning;
        }
    }
}
