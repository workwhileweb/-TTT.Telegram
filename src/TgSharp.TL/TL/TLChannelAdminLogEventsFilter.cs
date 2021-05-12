using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL
{
    [TLObject(-368018716)]
    public class TLChannelAdminLogEventsFilter : TLObject
    {
        public override int Constructor
        {
            get
            {
                return -368018716;
            }
        }

        public int Flags { get; set; }
        public bool Join { get; set; }
        public bool Leave { get; set; }
        public bool Invite { get; set; }
        public bool Ban { get; set; }
        public bool Unban { get; set; }
        public bool Kick { get; set; }
        public bool Unkick { get; set; }
        public bool Promote { get; set; }
        public bool Demote { get; set; }
        public bool Info { get; set; }
        public bool Settings { get; set; }
        public bool Pinned { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool GroupCall { get; set; }
        public bool Invites { get; set; }

        public void ComputeFlags()
        {
            Flags = 0;
Flags = Join ? (Flags | 1) : (Flags & ~1);
Flags = Leave ? (Flags | 2) : (Flags & ~2);
Flags = Invite ? (Flags | 4) : (Flags & ~4);
Flags = Ban ? (Flags | 8) : (Flags & ~8);
Flags = Unban ? (Flags | 16) : (Flags & ~16);
Flags = Kick ? (Flags | 32) : (Flags & ~32);
Flags = Unkick ? (Flags | 64) : (Flags & ~64);
Flags = Promote ? (Flags | 128) : (Flags & ~128);
Flags = Demote ? (Flags | 256) : (Flags & ~256);
Flags = Info ? (Flags | 512) : (Flags & ~512);
Flags = Settings ? (Flags | 1024) : (Flags & ~1024);
Flags = Pinned ? (Flags | 2048) : (Flags & ~2048);
Flags = Edit ? (Flags | 4096) : (Flags & ~4096);
Flags = Delete ? (Flags | 8192) : (Flags & ~8192);
Flags = GroupCall ? (Flags | 16384) : (Flags & ~16384);
Flags = Invites ? (Flags | 32768) : (Flags & ~32768);

        }

        public override void DeserializeBody(BinaryReader br)
        {
            Flags = br.ReadInt32();
            Join = (Flags & 1) != 0;
            Leave = (Flags & 2) != 0;
            Invite = (Flags & 4) != 0;
            Ban = (Flags & 8) != 0;
            Unban = (Flags & 16) != 0;
            Kick = (Flags & 32) != 0;
            Unkick = (Flags & 64) != 0;
            Promote = (Flags & 128) != 0;
            Demote = (Flags & 256) != 0;
            Info = (Flags & 512) != 0;
            Settings = (Flags & 1024) != 0;
            Pinned = (Flags & 2048) != 0;
            Edit = (Flags & 4096) != 0;
            Delete = (Flags & 8192) != 0;
            GroupCall = (Flags & 16384) != 0;
            Invites = (Flags & 32768) != 0;
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            bw.Write(Flags);
        }
    }
}
