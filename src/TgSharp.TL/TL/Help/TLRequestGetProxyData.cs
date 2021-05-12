using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TgSharp.TL;

namespace TgSharp.TL.Help
{
    [TLObject(-1063816159)]
    public class TLRequestGetPromoData : TLMethod<Help.TLAbsPromoData>
    {
        public override int Constructor
        {
            get
            {
                return -1063816159;
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
            Response = (Help.TLAbsPromoData)ObjectUtils.DeserializeObject(br);
        }
    }
}
