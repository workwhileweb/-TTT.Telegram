using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL
{
    [TLObject(453805082)]
    public class TLDraftMessageEmpty : TLAbsDraftMessage
    {
        public override int Constructor
        {
            get
            {
                return 453805082;
            }
        }

        public int Flags { get; set; }
        public int? Date { get; set; }

        public void ComputeFlags()
        {
            Flags = 0;
Flags = Date != null ? (Flags | 1) : (Flags & ~1);

        }

        public override void DeserializeBody(BinaryReader br)
        {
            Flags = br.ReadInt32();
            if ((Flags & 1) != 0)
                Date = br.ReadInt32();
            else
                Date = null;

        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(Flags);
            if ((Flags & 1) != 0)
                bw.Write(Date.Value);
        }
    }
}
