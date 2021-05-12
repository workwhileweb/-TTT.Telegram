using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL
{
    [TLObject(1775479590)]
    public class TLUserProfilePhoto : TLAbsUserProfilePhoto
    {
        public override int Constructor
        {
            get
            {
                return 1775479590;
            }
        }

        public int Flags { get; set; }
        public bool HasVideo { get; set; }
        public long PhotoId { get; set; }
        public TLFileLocationToBeDeprecated PhotoSmall { get; set; }
        public TLFileLocationToBeDeprecated PhotoBig { get; set; }
        public int DcId { get; set; }

        public void ComputeFlags()
        {
            Flags = 0;
Flags = HasVideo ? (Flags | 1) : (Flags & ~1);

        }

        public override void DeserializeBody(BinaryReader br)
        {
            Flags = br.ReadInt32();
            HasVideo = (Flags & 1) != 0;
            PhotoId = br.ReadInt64();
            PhotoSmall = (TLFileLocationToBeDeprecated)ObjectUtils.DeserializeObject(br);
            PhotoBig = (TLFileLocationToBeDeprecated)ObjectUtils.DeserializeObject(br);
            DcId = br.ReadInt32();
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(Flags);
            bw.Write(PhotoId);
            ObjectUtils.SerializeObject(PhotoSmall, bw);
            ObjectUtils.SerializeObject(PhotoBig, bw);
            bw.Write(DcId);
        }
    }
}
