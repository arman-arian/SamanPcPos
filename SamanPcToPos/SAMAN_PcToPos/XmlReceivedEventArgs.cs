using System;

namespace SamanPcToPos.SAMAN_PcToPos
{
    public class XmlReceivedEventArgs : EventArgs
    {
        public XmlReceivedEventArgs(bool isSuccessful, string receivedXml, PosResponse posResponse)
        {
            this.IsSuccessful = isSuccessful;
            this.XmlRecieve = receivedXml;
            this.PosResponse = posResponse;
        }

        public bool IsSuccessful { get; set; }

        public PosResponse PosResponse { get; set; }

        public string XmlRecieve { get; set; }
    }
}

