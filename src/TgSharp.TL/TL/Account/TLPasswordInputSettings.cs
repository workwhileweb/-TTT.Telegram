using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL.Account
{
    [TLObject(-1036572727)]
    public class TLPasswordInputSettings : TLObject
    {
        public override int Constructor
        {
            get
            {
                return -1036572727;
            }
        }

        public int Flags { get; set; }
        public TLAbsPasswordKdfAlgo NewAlgo { get; set; }
        public byte[] NewPasswordHash { get; set; }
        public string Hint { get; set; }
        public string Email { get; set; }
        public TLSecureSecretSettings NewSecureSettings { get; set; }

        public void ComputeFlags()
        {
            Flags = 0;
Flags = NewAlgo != null ? (Flags | 1) : (Flags & ~1);
Flags = NewPasswordHash != null ? (Flags | 1) : (Flags & ~1);
Flags = Hint != null ? (Flags | 1) : (Flags & ~1);
Flags = Email != null ? (Flags | 2) : (Flags & ~2);
Flags = NewSecureSettings != null ? (Flags | 4) : (Flags & ~4);

        }

        public override void DeserializeBody(BinaryReader br)
        {
            Flags = br.ReadInt32();
            if ((Flags & 1) != 0)
                NewAlgo = (TLAbsPasswordKdfAlgo)ObjectUtils.DeserializeObject(br);
            else
                NewAlgo = null;

            if ((Flags & 1) != 0)
                NewPasswordHash = BytesUtil.Deserialize(br);
            else
                NewPasswordHash = null;

            if ((Flags & 1) != 0)
                Hint = StringUtil.Deserialize(br);
            else
                Hint = null;

            if ((Flags & 2) != 0)
                Email = StringUtil.Deserialize(br);
            else
                Email = null;

            if ((Flags & 4) != 0)
                NewSecureSettings = (TLSecureSecretSettings)ObjectUtils.DeserializeObject(br);
            else
                NewSecureSettings = null;

        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(Flags);
            if ((Flags & 1) != 0)
                ObjectUtils.SerializeObject(NewAlgo, bw);
            if ((Flags & 1) != 0)
                BytesUtil.Serialize(NewPasswordHash, bw);
            if ((Flags & 1) != 0)
                StringUtil.Serialize(Hint, bw);
            if ((Flags & 2) != 0)
                StringUtil.Serialize(Email, bw);
            if ((Flags & 4) != 0)
                ObjectUtils.SerializeObject(NewSecureSettings, bw);
        }
    }
}
