using System;
using System.IO;

using TgSharp.TL;
using TgSharp.Core.Utils;

namespace TgSharp.Core.Network.Requests
{
    public class PingRequest : TLMethod<TLPong>
    {
        public PingRequest()
        {
        }

        public override void SerializeBody(BinaryWriter writer)
        {
            writer.Write(Constructor);
            writer.Write(Helpers.GenerateRandomLong());
        }

        public override void DeserializeBody(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        protected override void DeserializeResponse(BinaryReader stream)
        {
            Response = (TLPong)ObjectUtils.DeserializeObject(stream);
        }

        public override int Constructor
        {
            get
            {
                return 0x7abe77ec;
            }
        }
    }

    public class TLPong : TLObject
    {
        public long MessageId { get; set; }
        public long PingId { get; set; }

        public override int Constructor => 0x347773C5;

        public override void DeserializeBody(BinaryReader br)
        {
            MessageId = br.ReadInt64();
            PingId = br.ReadInt64();
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(MessageId);
            bw.Write(PingId);
        }
    }
}
