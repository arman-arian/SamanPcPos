using System.Collections.Generic;

namespace SamanPcToPos.SAMAN_PcToPos
{
    public class PublicItems
    {
        public PublicItems()
        {
            this.PrgVer = string.Empty;
            this.DllVer = string.Empty;
            this.TotalFee = "0";
            this.Amount1 = "0";
            this.Amount2 = "0";
            this.Amount3 = "0";
            this.Amount4 = "0";
            this.Amount5 = "0";
            this.Amount6 = "0";
            this.Amount7 = "0";
            this.Amount8 = "0";
            this.Amount9 = "0";
            this.Amount10 = "0";
            this.Amount11 = "0";
            this.Amount12 = "0";
            this.Amount13 = "0";
            this.Amount14 = "0";
            this.TestFee = "0";
            this.PItem = new List<PItem>();
        }

        public string Amount1 { get; set; }

        public string Amount10 { get; set; }

        public string Amount11 { get; set; }

        public string Amount12 { get; set; }

        public string Amount13 { get; set; }

        public string Amount14 { get; set; }

        public string Amount2 { get; set; }

        public string Amount3 { get; set; }

        public string Amount4 { get; set; }

        public string Amount5 { get; set; }

        public string Amount6 { get; set; }

        public string Amount7 { get; set; }

        public string Amount8 { get; set; }

        public string Amount9 { get; set; }

        public string DllVer { get; set; }

        public List<PItem> PItem { get; set; }

        public string PrgVer { get; set; }

        public string TestFee { get; set; }

        public string TotalFee { get; set; }
    }
}

