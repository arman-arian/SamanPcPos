using System.Collections.Generic;

namespace SamanPcToPos.SAMAN_PcToPos
{
    public class PostItems
    {
        public PostItems()
        {
            this.PrgVer = string.Empty;
            this.DllVer = string.Empty;
            this.VAT = "0";
            this.PostFee = "0";
            this.TrustFundFee = "0";
            this.TotalFee = "0";
            this.TestFee = "0";
        }

        public string DllVer { get; set; }

        public List<PItem> PItem { get; set; }

        public string PostFee { get; set; }

        public string PrgVer { get; set; }

        public string TestFee { get; set; }

        public string TotalFee { get; set; }

        public string TrustFundFee { get; set; }

        public string VAT { get; set; }
    }
}

