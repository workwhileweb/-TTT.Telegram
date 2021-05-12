using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL.Messages
{
    [TLObject(-241247891)]
    public class TLRequestGetDialogFilters : TLMethod<TLVector<TLDialogFilter>>
    {
        public override int Constructor
        {
            get
            {
                return -241247891;
            }
        }

        

        public void ComputeFlags()
        {
            // do nothing
        }

        public override void DeserializeBody(BinaryReader br)
        {
            // do nothing
        }

        public override void SerializeBody(BinaryWriter bw)
        {
            bw.Write(Constructor);
            // do nothing else
        }

        protected override void DeserializeResponse(BinaryReader br)
        {
            Response = (TLVector<TLDialogFilter>)ObjectUtils.DeserializeVector<TLDialogFilter>(br);
        }
    }
}
