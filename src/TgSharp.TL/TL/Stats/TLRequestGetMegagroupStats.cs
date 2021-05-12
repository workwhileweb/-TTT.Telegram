using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL.Stats
{
    [TLObject(-589330937)]
    public class TLRequestGetMegagroupStats : TLMethod<Stats.TLMegagroupStats>
    {
        public override int Constructor
        {
            get
            {
                return -589330937;
            }
        }

        public int Flags { get; set; }
        public bool Dark { get; set; }
        public TLAbsInputChannel Channel { get; set; }
        

        public void ComputeFlags()
        {
            Flags = 0;
Flags = Dark ? (Flags | 1) : (Flags & ~1);

        }

        public override void DeserializeBody(BinaryReader br)
        {
            Flags = br.ReadInt32();
            Dark = (Flags & 1) != 0;
            Channel = (TLAbsInputChannel)ObjectUtils.DeserializeObject(br);
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(Flags);
            ObjectUtils.SerializeObject(Channel, bw);
        }

        protected override void DeserializeResponse(BinaryReader br)
        {
            Response = (Stats.TLMegagroupStats)ObjectUtils.DeserializeObject(br);
        }
    }
}
