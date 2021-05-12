using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL
{
    [TLObject(1605510357)]
    public class TLChatAdminRights : TLObject
    {
        public override int Constructor
        {
            get
            {
                return 1605510357;
            }
        }

        public int Flags { get; set; }
        public bool ChangeInfo { get; set; }
        public bool PostMessages { get; set; }
        public bool EditMessages { get; set; }
        public bool DeleteMessages { get; set; }
        public bool BanUsers { get; set; }
        public bool InviteUsers { get; set; }
        public bool PinMessages { get; set; }
        public bool AddAdmins { get; set; }
        public bool Anonymous { get; set; }
        public bool ManageCall { get; set; }
        public bool Other { get; set; }

        public void ComputeFlags()
        {
            Flags = 0;
Flags = ChangeInfo ? (Flags | 1) : (Flags & ~1);
Flags = PostMessages ? (Flags | 2) : (Flags & ~2);
Flags = EditMessages ? (Flags | 4) : (Flags & ~4);
Flags = DeleteMessages ? (Flags | 8) : (Flags & ~8);
Flags = BanUsers ? (Flags | 16) : (Flags & ~16);
Flags = InviteUsers ? (Flags | 32) : (Flags & ~32);
Flags = PinMessages ? (Flags | 128) : (Flags & ~128);
Flags = AddAdmins ? (Flags | 512) : (Flags & ~512);
Flags = Anonymous ? (Flags | 1024) : (Flags & ~1024);
Flags = ManageCall ? (Flags | 2048) : (Flags & ~2048);
Flags = Other ? (Flags | 4096) : (Flags & ~4096);

        }

        public override void DeserializeBody(BinaryReader br)
        {
            Flags = br.ReadInt32();
            ChangeInfo = (Flags & 1) != 0;
            PostMessages = (Flags & 2) != 0;
            EditMessages = (Flags & 4) != 0;
            DeleteMessages = (Flags & 8) != 0;
            BanUsers = (Flags & 16) != 0;
            InviteUsers = (Flags & 32) != 0;
            PinMessages = (Flags & 128) != 0;
            AddAdmins = (Flags & 512) != 0;
            Anonymous = (Flags & 1024) != 0;
            ManageCall = (Flags & 2048) != 0;
            Other = (Flags & 4096) != 0;
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(Flags);
        }
    }
}
