using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL
{
    [TLObject(643940105)]
    public class TLUpdatePhoneCallSignalingData : TLAbsUpdate
    {
        public override int Constructor
        {
            get
            {
                return 643940105;
            }
        }

        public long PhoneCallId { get; set; }
        public byte[] Data { get; set; }

        public void ComputeFlags()
        {
            // do nothing
        }

        public override void DeserializeBody(BinaryReader br)
        {
            PhoneCallId = br.ReadInt64();
            Data = BytesUtil.Deserialize(br);
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(PhoneCallId);
            BytesUtil.Serialize(Data, bw);
        }
    }
}
