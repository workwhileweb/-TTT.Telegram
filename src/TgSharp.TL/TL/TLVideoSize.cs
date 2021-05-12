using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL
{
    [TLObject(-399391402)]
    public class TLVideoSize : TLObject
    {
        public override int Constructor
        {
            get
            {
                return -399391402;
            }
        }

        public int Flags { get; set; }
        public string Type { get; set; }
        public TLFileLocationToBeDeprecated Location { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public int Size { get; set; }
        public double? VideoStartTs { get; set; }

        public void ComputeFlags()
        {
            Flags = 0;
Flags = VideoStartTs != null ? (Flags | 1) : (Flags & ~1);

        }

        public override void DeserializeBody(BinaryReader br)
        {
            Flags = br.ReadInt32();
            Type = StringUtil.Deserialize(br);
            Location = (TLFileLocationToBeDeprecated)ObjectUtils.DeserializeObject(br);
            W = br.ReadInt32();
            H = br.ReadInt32();
            Size = br.ReadInt32();
            if ((Flags & 1) != 0)
                VideoStartTs = br.ReadDouble();
            else
                VideoStartTs = null;

        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(Flags);
            StringUtil.Serialize(Type, bw);
            ObjectUtils.SerializeObject(Location, bw);
            bw.Write(W);
            bw.Write(H);
            bw.Write(Size);
            if ((Flags & 1) != 0)
                bw.Write(VideoStartTs.Value);
        }
    }
}
