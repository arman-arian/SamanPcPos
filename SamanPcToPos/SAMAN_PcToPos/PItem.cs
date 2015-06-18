namespace SamanPcToPos.SAMAN_PcToPos
{
    public class PItem
    {
        public PItem(string _Code, string _Value, string _Printed)
        {
            this.Code = _Code;
            this.Value = _Value;
            this.Printed = _Printed;
        }

        public string Code { get; set; }

        public string Printed { get; set; }

        public string Value { get; set; }
    }
}

