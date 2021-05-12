using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL
{
    [TLObject(512177195)]
    public class TLDocument : TLAbsDocument
    {
        public override int Constructor
        {
            get
            {
                return 512177195;
            }
        }

        public int Flags { get; set; }
        public long Id { get; set; }
        public long AccessHash { get; set; }
        public byte[] FileReference { get; set; }
        public int Date { get; set; }
        public string MimeType { get; set; }
        public int Size { get; set; }
        public TLVector<TLAbsPhotoSize> Thumbs { get; set; }
        public TLVector<TLVideoSize> VideoThumbs { get; set; }
        public int DcId { get; set; }
        public TLVector<TLAbsDocumentAttribute> Attributes { get; set; }

        public void ComputeFlags()
        {
            Flags = 0;
Flags = Thumbs != null ? (Flags | 1) : (Flags & ~1);
Flags = VideoThumbs != null ? (Flags | 2) : (Flags & ~2);

        }

        public override void DeserializeBody(BinaryReader br)
        {
            Flags = br.ReadInt32();
            Id = br.ReadInt64();
            AccessHash = br.ReadInt64();
            FileReference = BytesUtil.Deserialize(br);
            Date = br.ReadInt32();
            MimeType = StringUtil.Deserialize(br);
            Size = br.ReadInt32();
            if ((Flags & 1) != 0)
                Thumbs = (TLVector<TLAbsPhotoSize>)ObjectUtils.DeserializeVector<TLAbsPhotoSize>(br);
            else
                Thumbs = null;

            if ((Flags & 2) != 0)
                VideoThumbs = (TLVector<TLVideoSize>)ObjectUtils.DeserializeVector<TLVideoSize>(br);
            else
                VideoThumbs = null;

            DcId = br.ReadInt32();
            Attributes = (TLVector<TLAbsDocumentAttribute>)ObjectUtils.DeserializeVector<TLAbsDocumentAttribute>(br);
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(Flags);
            bw.Write(Id);
            bw.Write(AccessHash);
            BytesUtil.Serialize(FileReference, bw);
            bw.Write(Date);
            StringUtil.Serialize(MimeType, bw);
            bw.Write(Size);
            if ((Flags & 1) != 0)
                ObjectUtils.SerializeObject(Thumbs, bw);
            if ((Flags & 2) != 0)
                ObjectUtils.SerializeObject(VideoThumbs, bw);
            bw.Write(DcId);
            ObjectUtils.SerializeObject(Attributes, bw);
        }
    }
}
