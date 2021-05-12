using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL
{
    [TLObject(1611985938)]
    public class TLStatsGroupTopAdmin : TLObject
    {
        public override int Constructor
        {
            get
            {
                return 1611985938;
            }
        }

        public int UserId { get; set; }
        public int Deleted { get; set; }
        public int Kicked { get; set; }
        public int Banned { get; set; }

        public void ComputeFlags()
        {
            // do nothing
        }

        public override void DeserializeBody(BinaryReader br)
        {
            UserId = br.ReadInt32();
            Deleted = br.ReadInt32();
            Kicked = br.ReadInt32();
            Banned = br.ReadInt32();
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(UserId);
            bw.Write(Deleted);
            bw.Write(Kicked);
            bw.Write(Banned);
        }
    }
}
