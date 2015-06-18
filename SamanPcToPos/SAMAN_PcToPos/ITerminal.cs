using System.Runtime.InteropServices;

namespace SamanPcToPos.SAMAN_PcToPos
{
    [InterfaceType(ComInterfaceType.InterfaceIsDual), Guid("830BD354-EAAC-4DC5-8610-9D43E0CFF8FB"), ComVisible(true)]
    public interface ITerminal
    {
        bool IsPortOpen { get; }
        void Init();
        bool OpenPort();
        void ClosePort();
        bool OpenSocket();
        void CloseSocket();
        bool SendToCOM(string xml);
        bool SendToSocket(string xml);
        void SetPort(string PortName);
        string GetXmlRecieveState();
        string GetXmlRecieve();
        string GetXmlError();
        void SetConfirmFlag(bool i);
        void SetPrintFlag(byte i);
    }
}

