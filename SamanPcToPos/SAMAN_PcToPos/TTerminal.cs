using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using SamanPcToPos.Helper;
using SamanPcToPos.ISOHelper.Util;
using SamanPcToPos.TLV;

namespace SamanPcToPos.SAMAN_PcToPos
{
    [ComVisible(true), Guid("5E9C1704-28BC-498c-A54E-A19F082D08B6"), ComSourceInterfaces(typeof (ITerminal)),
     ClassInterface(ClassInterfaceType.None)]
    public class TTerminal : ITerminal, IDisposable
    {
        private static SerialPort __comPort;
        protected long _Amount1;
        protected long _Amount10;
        protected long _Amount11;
        protected long _Amount12;
        protected long _Amount13;
        protected long _Amount14;
        protected long _Amount2;
        protected long _Amount3;
        protected long _Amount4;
        protected long _Amount5;
        protected long _Amount6;
        protected long _Amount7;
        protected long _Amount8;
        protected long _Amount9;
        protected long _AmountTest;
        protected bool _confirmFlag;
        protected bool _isPortOpen;
        protected string _prg;
        protected byte _printFlag;
        protected string _receivdText;
        protected string _reciptPrint;
        protected long _ToTalAmount;
        protected string _xmlError;
        protected string _xmlRecieve;
        protected string _xmlRecieveState;
        private TcpClient client;
        private Thread clientThread;
        protected byte[] finalMessage;
        private bool HasPurchase;
        private bool HasSegregation;
        protected List<string[]> InfoItems;
        private Thread listenThread;
        protected byte[] NoMessage;
        protected List<PurchaseItem> PurchaseItems;
        protected byte[] receivdMsg;
        protected string RetActionCode;
        protected string RetAffectedAmount;
        protected string RetAmount;
        protected string RetBatchNo;
        protected string RetDateTime;
        protected string RetPAN;
        protected string RetResponseCode;
        protected string RetRRN;
        protected string RetSerialNo;
        protected string RetTermID;
        protected string RetTraceNo;
        private TcpListener tcpListener;
        protected byte[] YesMessage;

        public event EventHandler<XmlReceivedEventArgs> XMLReceived;

        public TTerminal()
        {
            this.InfoItems = new List<string[]>();
            this.PurchaseItems = new List<PurchaseItem>();
            this.YesMessage = new byte[] {2, 8, 0, 0, 0, 1, 0x72, 6, 0xb1, 4, 0x88, 2, 0, 1};
            this.NoMessage = new byte[] {2, 8, 0, 0, 0, 1, 0x72, 6, 0xb1, 4, 0x88, 2, 0, 2};
            this._xmlError = string.Empty;
            this._xmlRecieveState = string.Empty;
            this._xmlRecieve = string.Empty;
            this._receivdText = string.Empty;
            this.RetPAN = string.Empty;
            this.RetBatchNo = string.Empty;
            this.RetSerialNo = string.Empty;
            this.RetDateTime = string.Empty;
            this.RetTermID = string.Empty;
            this.RetActionCode = string.Empty;
            this.RetResponseCode = string.Empty;
            this.RetRRN = string.Empty;
            this.RetTraceNo = string.Empty;
            this.RetAmount = string.Empty;
            this.RetAffectedAmount = string.Empty;
            this.Init();
            this.ShowMessages = true;
        }

        public TTerminal(string PortName) : this()
        {
            this.Port = PortName;
        }

        private void AddInfoItem(string ItemName, string ItemValue)
        {
            this.InfoItems.Add(new[] {ItemName, ItemValue});
        }

        private void AddPurchaseItem(string Code, string Serial, string Quantity, string Fee, string Name)
        {
            PurchaseItem item = new PurchaseItem
            {
                Code = Code,
                Serial = Serial,
                Quantity = Quantity,
                Fee = Fee,
                Name = Name
            };
            this.PurchaseItems.Add(item);
        }

        public void ClosePort()
        {
            if (this._isPortOpen)
            {
                comPort.Close();
                this._isPortOpen = false;
            }
        }

        public void CloseSocket()
        {
            if (this._isPortOpen && (this.listenThread.ThreadState == ThreadState.Running))
            {
                this.listenThread.Abort();
                if ((this.clientThread != null) && (this.clientThread.ThreadState == ThreadState.Running))
                {
                    this.clientThread.Abort();
                }
                this.tcpListener.Stop();
                if (this.client != null)
                {
                    this.client.Close();
                }
                this._isPortOpen = false;
            }
        }

        private void comPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(0x3e8);
            byte[] buffer = new byte[comPort.BytesToRead];
            int num = comPort.Read(buffer, 0, comPort.BytesToRead);
            this.receivdMsg = new byte[num - 6];
            Array.Copy(buffer, 6, this.receivdMsg, 0, num - 6);
            ISOUtil.hexString(this.receivdMsg);
            this.InitMsgReceiver();
            if (this.ConfirmFlag)
            {
                if (this.RetResponseCode == "0")
                {
                    if (MessageBox.Show("پرداخت فوق را تایید می نمایید ؟", "", MessageBoxButtons.YesNo) ==
                        DialogResult.Yes)
                    {
                        comPort.Write(this.YesMessage, 0, this.YesMessage.Length);
                        this.ShowReceiveMessage();
                    }
                    else
                    {
                        comPort.Write(this.NoMessage, 0, this.NoMessage.Length);
                        this.XmlRecieveState = "XMLRecive";
                        this._xmlRecieve = "<ConfirmTransaction>NO</ConfirmTransaction>";
                    }
                }
                else
                {
                    this.ShowReceiveMessage();
                }
            }
            else
            {
                this.ShowReceiveMessage();
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        private void CreateReciptPrintMizan(MizanItems oMizanItems)
        {
            this._reciptPrint = "";
            this._reciptPrint = "".PadRight(0x17, '-') + this._reciptPrint;
            if (!string.IsNullOrEmpty(oMizanItems.Code1))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee1).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code1).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name1).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code2))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee2).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code2).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name2).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code3))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee3).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code3).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name3).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code4))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee4).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code4).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name4).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code5))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee5).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code5).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name5).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code6))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee6).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code6).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name6).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code7))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee7).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code7).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name7).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code8))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee8).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code8).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name8).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code9))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee9).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code9).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name9).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            if (!string.IsNullOrEmpty(oMizanItems.Code10))
            {
                this._reciptPrint = ("فی:" + oMizanItems.Fee10).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("ایران کد" + oMizanItems.Code10).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    ("نام:" + oMizanItems.Name10).PadRight(0x18, ' ').Substring(0, 0x18) +
                                    this._reciptPrint;
            }
            this._reciptPrint = "".PadRight(0x18, '-') +
                                ("ارزش افزوده: " + oMizanItems.TaxFee).PadRight(0x18, ' ').Substring(0, 0x18) +
                                ("جمع فاکتور: " + oMizanItems.TotalFee).PadRight(0x18, ' ').Substring(0, 0x18) +
                                "".PadRight(0x18, '-') + this._reciptPrint;
            this._reciptPrint = this._reciptPrint.Trim();
        }

        private void CreateReciptPrintPublic(PublicItems oPublicItems)
        {
            this._reciptPrint = "";
            foreach (var item in oPublicItems.PItem)
            {
                if (item.Printed == "1")
                {
                    if (string.Equals(this.Prg.ToLower(), "khazaneh"))
                    {
                        string str = item.Code + ":" + item.Value;
                        this._reciptPrint = str.PadRight(0x18, ' ').Substring(0, 0x18) + this._reciptPrint;
                    }
                    else
                    {
                        string str2 = item.Code + ":" + item.Value + "\n";
                        this._reciptPrint = (str2.Length > 0x18)
                            ? (str2.Substring(0, 0x18) + this._reciptPrint)
                            : (str2 + this._reciptPrint);
                    }
                }
            }
        }

        private void CreateReciptPrintSabt(byte kind, SabtItems oSabtItems)
        {
            switch (kind)
            {
                case 1:
                    this._reciptPrint =
                        ("شماره مرجع:" + oSabtItems.ReferenceNumber.Substring(0, 9)).PadRight(0x18, ' ')
                            .Substring(0, 0x18) +
                        ("شماره سند:" + oSabtItems.DocumentNumber).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("++++++" + oSabtItems.DocumentTypeNameLevel2).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("نوع سند:" + oSabtItems.DocumentTypeNameLevel1).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("صدور الکترونیک:" + oSabtItems.ElectronicIssuance).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("مالیات:" + oSabtItems.TaxCar).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("حق التحریر:" + oSabtItems.NotaryFees).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("حق الثبت:" + oSabtItems.RegistrationFee).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("شماره ملی:" + oSabtItems.VendeeNationalCode).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("متعامل:" + oSabtItems.VendeeName).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("شماره ملی:" + oSabtItems.VendorNationalCode).PadRight(0x18, ' ').Substring(0, 0x18) +
                        ("معامل:" + oSabtItems.VendorName).PadRight(0x18, ' ').Substring(0, 0x18);
                    return;

                case 2:
                {
                    string[] strArray2 = new string[]
                    {
                        ("شماره مرجع:" + oSabtItems.ReferenceNumber.Substring(0, 9)).PadRight(0x18, ' ')
                            .Substring(0, 0x18),
                        ("حق التحریر:" + oSabtItems.NotaryFees).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("سایر درآمدها:" + oSabtItems.OtherRegistrationFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("مبلغ:" + oSabtItems.ServiceFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("تعداد:" + oSabtItems.ServiceNumber).PadRight(0x18, ' ').Substring(0, 0x18),
                        (oSabtItems.ServiceTypeName ?? "").PadRight(0x18, ' ').Substring(0, 0x18)
                    };
                    this._reciptPrint = string.Concat(strArray2);
                    return;
                }
                case 3:
                {
                    string[] strArray3 = new string[]
                    {
                        ("شماره مرجع:" + oSabtItems.ReferenceNumber.Substring(0, 9)).PadRight(0x18, ' ')
                            .Substring(0, 0x18),
                        ("حق التحریر:" + oSabtItems.NotaryFees).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("سایر درآمدها:" + oSabtItems.OtherRegistrationFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("مبلغ:" + oSabtItems.ServiceFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("تعداد:" + oSabtItems.ServiceNumber).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("تا شماره:" + oSabtItems.EndSignWitness).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("از شماره:" + oSabtItems.StartSignWitness).PadRight(0x18, ' ').Substring(0, 0x18),
                        (oSabtItems.ServiceTypeName ?? "").PadRight(0x18, ' ').Substring(0, 0x18)
                    };
                    this._reciptPrint = string.Concat(strArray3);
                    return;
                }
                case 4:
                {
                    string[] strArray4 = new string[]
                    {
                        ("شماره مرجع:" + oSabtItems.ReferenceNumber.Substring(0, 9)).PadRight(0x18, ' ')
                            .Substring(0, 0x18),
                        ("حق التحریر:" + oSabtItems.NotaryFees).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("استعلام:" + oSabtItems.InquiryFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("کد ملی:" + oSabtItems.VendorNationalCode).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("نام:" + oSabtItems.VendorName).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("مبلغ:" + oSabtItems.ServiceFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("تعداد:" + oSabtItems.ServiceNumber).PadRight(0x18, ' ').Substring(0, 0x18),
                        (oSabtItems.ServiceTypeName ?? "").PadRight(0x18, ' ').Substring(0, 0x18)
                    };
                    this._reciptPrint = string.Concat(strArray4);
                    return;
                }
                case 5:
                {
                    string[] strArray5 = new string[]
                    {
                        ("شماره مرجع:" + oSabtItems.ReferenceNumber.Substring(0, 9)).PadRight(0x18, ' ')
                            .Substring(0, 0x18),
                        ("حق التحریر:" + oSabtItems.NotaryFees).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("بقایای ثبتی:" + oSabtItems.RemainingFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        "".PadRight(0x18, ' ').Substring(0, 0x18),
                        (oSabtItems.ServiceTypeName ?? "").PadRight(0x18, ' ').Substring(0, 0x18)
                    };
                    this._reciptPrint = string.Concat(strArray5);
                    return;
                }
                case 6:
                {
                    string[] strArray6 = new string[]
                    {
                        ("شماره مرجع:" + oSabtItems.ReferenceNumber.Substring(0, 9)).PadRight(0x18, ' ')
                            .Substring(0, 0x18),
                        ("حق الثبت:" + oSabtItems.RegistrationFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("بهای اوراق:" + oSabtItems.PagesPriceFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("هزینه تعویض سند:" + oSabtItems.ChangingFee).PadRight(0x18, ' ').Substring(0, 0x18),
                        ("حق التحریر:" + oSabtItems.NotaryFees).PadRight(0x18, ' ').Substring(0, 0x18),
                        (oSabtItems.ServiceTypeName ?? "").PadRight(0x18, ' ').Substring(0, 0x18)
                    };
                    this._reciptPrint = string.Concat(strArray6);
                    return;
                }
                case 7:
                    this._reciptPrint =
                        ("شماره مرجع:" + oSabtItems.ReferenceNumber.Substring(0, 9)).PadRight(0x18, ' ')
                            .Substring(0, 0x18) +
                        ("تراكنش تستي:" + oSabtItems.TestFee).PadRight(0x18, ' ').Substring(0, 0x18);
                    return;
            }
        }

        public void Dispose()
        {
            this.ClosePort();
        }

        public string getString(string xml)
        {
            try
            {
                if (this.InitMessage(xml))
                {
                    return ISOUtil.hexString(this.finalMessage);
                }
            }
            catch
            {
                return "";
            }
            return "";
        }

        public string GetXmlError()
        {
            return this._xmlError;
        }

        public string GetXmlRecieve()
        {
            return this._xmlRecieve;
        }

        public string GetXmlRecieveState()
        {
            return this._xmlRecieveState;
        }

        [STAThread]
        // ReSharper disable once ParameterHidesMember
        private void HandleClientComm(object client)
        {
            var stream = ((TcpClient) client).GetStream();
            while (true)
            {
                int num;
                try
                {
                    var buffer = new byte[0x400];
                    num = stream.Read(buffer, 0, buffer.Length);
                    this.receivdMsg = new byte[num - 1];
                    Array.Copy(buffer, 1, this.receivdMsg, 0, num - 1);
                    ISOUtil.hexString(this.receivdMsg);
                }
                catch
                {
                    return;
                }
                if (num == 0)
                {
                    return;
                }
                if (num <= 10) continue;
                this.InitMsgReceiver();
                if (this.ConfirmFlag)
                {
                    if (this.RetResponseCode == "0")
                    {
                        if (MessageBox.Show("پرداخت فوق را تایید می نمایید ؟", "", MessageBoxButtons.YesNo) ==
                            DialogResult.Yes)
                        {
                            stream.Write(new byte[] {2, 8, 0, 0, 0, 1, 0x72, 6, 0xb1, 4, 0x88, 2, 0, 1}, 0, 14);
                            stream.Flush();
                            this.ShowReceiveMessage();
                        }
                        else
                        {
                            stream.Write(new byte[] {2, 8, 0, 0, 0, 1, 0x72, 6, 0xb1, 4, 0x88, 2, 0, 3}, 0, 14);
                            stream.Flush();
                            this._xmlRecieveState = "XMLRecive";
                            this._xmlRecieve = "<ConfirmTransaction>NO</ConfirmTransaction>";
                        }
                    }
                    else
                    {
                        this.ShowReceiveMessage();
                    }
                }
                else
                {
                    this.ShowReceiveMessage();
                }
            }
        }

        public void Init()
        {
            comPort = new SerialPort {BaudRate = 0x4b00, Parity = Parity.None, DataBits = 8, StopBits = StopBits.One};
            comPort.DataReceived += this.comPort_DataReceived;
        }

        // ReSharper disable once FunctionComplexityOverflow
        protected bool InitMessage(string xml)
        {
            this._xmlRecieveState = string.Empty;
            if (!this.ParseInputXML(xml))
            {
                return false;
            }

            try
            {
                if (this.HasSegregation &&
                    (((((((((((((((this._Amount1 + this._Amount2) + this._Amount3) + this._Amount4) + this._Amount5) +
                               this._Amount6) + this._Amount7) + this._Amount8) + this._Amount9) + this._Amount10) +
                          this._Amount11) + this._Amount12) + this._Amount13) + this._Amount14) + this._AmountTest) !=
                     this._ToTalAmount))
                {
                    this._xmlError = "مجموع مبالغ تفکیکی با مبلغ کل برابر نیست";
                    return false;
                }
                var msg = new TLVMsg(0x72, null, "");
                var msg2 = new TLVMsg(0xb1, null, "");
                var msg3 = new TLVMsg(0xb2, null, "");
                var msg4 = new TLVMsg(0xa1, null, "");
                var msg5 = new TLVMsg(0xa2, null, "");
                var msg6 = new TLVMsg(0xa3, null, "");
                foreach (string[] strArray in this.InfoItems)
                {
                    var msg7 = new TLVMsg(0xa1, null, "");
                    var str = string.IsNullOrEmpty(strArray[1]) ? "_" : strArray[1];
                    var msg8 = new TLVMsg(0x81, Encoding.GetEncoding(0x4e8).GetBytes(strArray[0]), "");
                    var msg9 = new TLVMsg(130, Encoding.GetEncoding(0x4e8).GetBytes(str), "");
                    msg7.AddChild(msg8);
                    msg7.AddChild(msg9);
                    msg5.AddChild(msg7);
                }
                foreach (PurchaseItem item in this.PurchaseItems)
                {
                    var msg10 = new TLVMsg(0xa1, null, "");
                    var msg11 = new TLVMsg(0x81, Encoding.GetEncoding(0x4e8).GetBytes(item.Code), "");
                    var msg12 = new TLVMsg(130,
                        Encoding.GetEncoding(0x4e8).GetBytes(string.IsNullOrEmpty(item.Serial) ? " " : item.Serial), "");
                    var msg13 = new TLVMsg(0x83, Encoding.GetEncoding(0x4e8).GetBytes(item.Quantity), "");
                    var msg14 = new TLVMsg(0x84, Encoding.GetEncoding(0x4e8).GetBytes(item.Fee), "");
                    msg10.AddChild(msg11);
                    msg10.AddChild(msg12);
                    msg10.AddChild(msg13);
                    msg10.AddChild(msg14);
                    msg6.AddChild(msg10);
                }
                var msg15 = new TLVMsg(0x81, Encoding.GetEncoding(0x4e8).GetBytes(this._ToTalAmount.ToString()), "");
                var msg16 = new TLVMsg(130, Encoding.GetEncoding(0x4e8).GetBytes(this._reciptPrint), "");
                var msg17 = new TLVMsg(0x83, new byte[] {1}, "");
                var msg18 = new TLVMsg(0x84, new[] {this.PrintFlag}, "");
                var msg19 = new TLVMsg(0x85, new[] {this.ConfirmFlag ? ((byte) 1) : ((byte) 0)}, "");
                var s = string.IsNullOrEmpty(this._Amount1.ToString()) ? "0" : this._Amount1.ToString();
                var str3 = string.IsNullOrEmpty(this._Amount2.ToString()) ? "0" : this._Amount2.ToString();
                var str4 = string.IsNullOrEmpty(this._Amount3.ToString()) ? "0" : this._Amount3.ToString();
                var str5 = string.IsNullOrEmpty(this._Amount4.ToString()) ? "0" : this._Amount4.ToString();
                var str6 = string.IsNullOrEmpty(this._Amount5.ToString()) ? "0" : this._Amount5.ToString();
                var str7 = string.IsNullOrEmpty(this._Amount6.ToString()) ? "0" : this._Amount6.ToString();
                var str8 = string.IsNullOrEmpty(this._Amount7.ToString()) ? "0" : this._Amount7.ToString();
                var str9 = string.IsNullOrEmpty(this._Amount8.ToString()) ? "0" : this._Amount8.ToString();
                var str10 = string.IsNullOrEmpty(this._Amount9.ToString()) ? "0" : this._Amount9.ToString();
                var str11 = string.IsNullOrEmpty(this._Amount10.ToString()) ? "0" : this._Amount10.ToString();
                var str12 = string.IsNullOrEmpty(this._Amount11.ToString()) ? "0" : this._Amount11.ToString();
                var str13 = string.IsNullOrEmpty(this._Amount12.ToString()) ? "0" : this._Amount12.ToString();
                var str14 = string.IsNullOrEmpty(this._Amount13.ToString()) ? "0" : this._Amount13.ToString();
                var str15 = string.IsNullOrEmpty(this._Amount14.ToString()) ? "0" : this._Amount14.ToString();
                var str16 = string.IsNullOrEmpty(this._AmountTest.ToString()) ? "0" : this._AmountTest.ToString();
                var msg20 = new TLVMsg(0x81, Encoding.GetEncoding(0x4e8).GetBytes(s), "");
                var msg21 = new TLVMsg(130, Encoding.GetEncoding(0x4e8).GetBytes(str3), "");
                var msg22 = new TLVMsg(0x83, Encoding.GetEncoding(0x4e8).GetBytes(str4), "");
                var msg23 = new TLVMsg(0x84, Encoding.GetEncoding(0x4e8).GetBytes(str5), "");
                var msg24 = new TLVMsg(0x85, Encoding.GetEncoding(0x4e8).GetBytes(str6), "");
                var msg25 = new TLVMsg(0x86, Encoding.GetEncoding(0x4e8).GetBytes(str7), "");
                var msg26 = new TLVMsg(0x87, Encoding.GetEncoding(0x4e8).GetBytes(str8), "");
                var msg27 = new TLVMsg(0x88, Encoding.GetEncoding(0x4e8).GetBytes(str9), "");
                var msg28 = new TLVMsg(0x89, Encoding.GetEncoding(0x4e8).GetBytes(str10), "");
                var msg29 = new TLVMsg(0x8a, Encoding.GetEncoding(0x4e8).GetBytes(str11), "");
                var msg30 = new TLVMsg(0x8b, Encoding.GetEncoding(0x4e8).GetBytes(str12), "");
                var msg31 = new TLVMsg(140, Encoding.GetEncoding(0x4e8).GetBytes(str13), "");
                var msg32 = new TLVMsg(0x8d, Encoding.GetEncoding(0x4e8).GetBytes(str14), "");
                var msg33 = new TLVMsg(0x8e, Encoding.GetEncoding(0x4e8).GetBytes(str15), "");
                var msg34 = new TLVMsg(0x8f, Encoding.GetEncoding(0x4e8).GetBytes(str16), "");
                msg2.AddChild(msg15);
                msg2.AddChild(msg16);
                msg2.AddChild(msg17);
                msg2.AddChild(msg18);
                msg2.AddChild(msg19);
                if (this.HasSegregation)
                {
                    msg4.AddChild(msg20);
                    msg4.AddChild(msg21);
                    msg4.AddChild(msg22);
                    msg4.AddChild(msg23);
                    msg4.AddChild(msg24);
                    msg4.AddChild(msg25);
                    msg4.AddChild(msg26);
                    msg4.AddChild(msg27);
                    if (this._Amount9 > 0L)
                    {
                        msg4.AddChild(msg28);
                    }
                    if (this._Amount10 > 0L)
                    {
                        msg4.AddChild(msg29);
                    }
                    if (this._Amount11 > 0L)
                    {
                        msg4.AddChild(msg30);
                    }
                    if (this._Amount12 > 0L)
                    {
                        msg4.AddChild(msg31);
                    }
                    if (this._Amount13 > 0L)
                    {
                        msg4.AddChild(msg32);
                    }
                    if (this._Amount14 > 0L)
                    {
                        msg4.AddChild(msg33);
                    }
                    if (this._AmountTest > 0L)
                    {
                        msg4.AddChild(msg34);
                    }
                    msg3.AddChild(msg4);
                }
                msg3.AddChild(msg5);
                if (this.HasPurchase)
                {
                    msg3.AddChild(msg6);
                }
                msg.AddChild(msg2);
                msg.AddChild(msg3);
                msg3.CalcLength();
                byte[] buffer = msg.getTLV();
                byte[] buffer2 = new byte[4];
                if (buffer.Length < 0x100)
                {
                    buffer2[0] = (byte) ((buffer.Length + 4) & 0xff);
                }
                else if (buffer.Length < 0x10000)
                {
                    buffer2[1] = (byte) ((buffer.Length + 4) >> 8);
                    buffer2[0] = (byte) ((buffer.Length + 4) & 0xff);
                }
                byte[] sourceArray = ISOUtil.CalculateMAC(new byte[] {0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11},
                    msg3.getTLV());
                byte[] destinationArray = new byte[4];
                Array.Copy(sourceArray, destinationArray, 4);
                this.finalMessage = Myfunc.JoinArrays(new byte[] {2}, buffer2, new byte[] {1}, buffer, destinationArray);
                ISOUtil.hexString(this.finalMessage);
                this._xmlError = string.Empty;
                return true;
            }
            catch
            {
                this._xmlError = "خطا در ارسال اطلاعات";
                return false;
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        protected void InitMsgReceiver()
        {
            TLVMsg msg = new TLVMsg();
            this.receivdText = ISOUtil.hexString(this.receivdMsg);
            msg.unpack(this.receivdMsg);
            DateTime now = DateTime.Now;
            string str = now.Year + now.Month.ToString().PadLeft(2, '0') + now.Day.ToString().PadLeft(2, '0') +
                         now.Hour.ToString().PadLeft(2, '0') + now.Minute.ToString().PadLeft(2, '0') +
                         now.Second.ToString().PadLeft(2, '0');
            string path = Application.StartupPath + @"\log";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            TextWriter writer = new StreamWriter(path + @"\dll-" + str);
            writer.WriteLine("Sended TLV");
            writer.WriteLine(ISOUtil.hexString(this.finalMessage));
            writer.WriteLine("Recieved TLV");
            writer.WriteLine(this.receivdText);
            writer.Close();
            try
            {
                this.RetSerialNo =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x85, 1).getStringValue()));
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetBatchNo =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x86, 1).getStringValue()));
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetDateTime =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x87, 1).getStringValue()));
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetActionCode = msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x88, 1).getValue()[0].ToString();
                this.RetResponseCode =
                    msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x88, 1).getValue()[1].ToString();
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetTermID =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x89, 1).getStringValue()));
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetPAN =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x8a, 1).getStringValue()));
                this.RetPAN = string.Format("{0}****{1}", this.RetPAN.Substring(0, 6),
                    this.RetPAN.Substring(this.RetPAN.Length - 4, 4));
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetRRN =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x8b, 1).getStringValue()));
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetTraceNo =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(140, 1).getStringValue()));
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetAmount =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x8d, 1).getStringValue()));
            }
            catch
            {
                // ignored
            }
            try
            {
                this.RetAffectedAmount =
                    Encoding.ASCII.GetString(
                        ISOUtil.hex2byte(msg.getChild(0x72, 1).getChild(0xb1, 1).getChild(0x8e, 1).getStringValue()));
            }
            catch
            {
                // ignored
            }
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();
            while (true)
            {
                this.client = this.tcpListener.AcceptTcpClient();
                this.clientThread = new Thread(this.HandleClientComm);
                this.clientThread.Start(this.client);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public bool OpenPort()
        {
            if (!this._isPortOpen)
            {
                try
                {
                    comPort.PortName = this.Port;
                    comPort.Open();
                    comPort.DiscardInBuffer();
                    comPort.DiscardOutBuffer();
                    this._isPortOpen = true;
                    return true;
                }
                catch
                {
                    this._isPortOpen = false;
                    return false;
                }
            }
            this._isPortOpen = false;
            return false;
        }

        public bool OpenSocket()
        {
            if (!this._isPortOpen)
            {
                try
                {
                    this.tcpListener = new TcpListener(IPAddress.Any, Convert.ToInt32(this.Port));
                    this.listenThread = new Thread(this.ListenForClients);
                    this.listenThread.Start();
                    this._xmlError = string.Empty;
                    this._isPortOpen = true;
                    return true;
                }
                catch
                {
                    this._xmlError = "اشکال در باز کردن پورت";
                    this._isPortOpen = false;
                    return false;
                }
            }
            this._xmlError = "اشکال در باز کردن پورت";
            this._isPortOpen = false;
            return false;
        }

        protected bool ParseInputXML(string xml)
        {
            switch (this.Prg)
            {
                case "public":
                case "khazaneh":
                    this.HasSegregation = true;
                    this.HasPurchase = false;
                    return this.ParseInputXMLPublic(xml);

                case "sabt":
                    this.HasSegregation = true;
                    this.HasPurchase = false;
                    return this.ParseInputXMLSabt(xml);
            }
            return false;
        }

        // ReSharper disable once FunctionComplexityOverflow
        protected bool ParseInputXMLMizan(string xml)
        {
            MizanItems oMizanItems = new MizanItems();
            try
            {
                var document = new XmlDocument();
                document.LoadXml(string.Format("<test>{0}</test>", xml));
                var documentElement = document.DocumentElement;
                if (documentElement != null && documentElement.SelectSingleNode("Code1") != null)
                {
                    var selectSingleNode = documentElement.SelectSingleNode("Code1");
                    if (selectSingleNode != null)
                        oMizanItems.Code1 = selectSingleNode.InnerText;
                    var selectSingleNode1 = documentElement.SelectSingleNode("Serial1");
                    if (selectSingleNode1 != null)
                        oMizanItems.Serial1 = selectSingleNode1.InnerText;
                    var node = documentElement.SelectSingleNode("Quantity1");
                    if (node != null)
                        oMizanItems.Quantity1 = node.InnerText;
                    var xmlNode = documentElement.SelectSingleNode("Fee1");
                    if (xmlNode != null)
                        oMizanItems.Fee1 = xmlNode.InnerText;
                    var singleNode = documentElement.SelectSingleNode("Name1");
                    if (singleNode != null)
                        oMizanItems.Name1 = singleNode.InnerText;
                }
                if (documentElement != null && documentElement.SelectSingleNode("Code2") != null)
                {
                    var selectSingleNode = documentElement.SelectSingleNode("Code2");
                    if (selectSingleNode != null)
                        oMizanItems.Code2 = selectSingleNode.InnerText;
                    var singleNode = documentElement.SelectSingleNode("Serial2");
                    if (singleNode != null)
                        oMizanItems.Serial2 = singleNode.InnerText;
                    var xmlNode = documentElement.SelectSingleNode("Quantity2");
                    if (xmlNode != null)
                        oMizanItems.Quantity2 = xmlNode.InnerText;
                    var selectSingleNode1 = documentElement.SelectSingleNode("Fee2");
                    if (selectSingleNode1 != null)
                        oMizanItems.Fee2 = selectSingleNode1.InnerText;
                    var node = documentElement.SelectSingleNode("Name2");
                    if (node != null)
                        oMizanItems.Name2 = node.InnerText;
                }
                if (documentElement != null && documentElement.SelectSingleNode("Code3") != null)
                {
                    var selectSingleNode = documentElement.SelectSingleNode("Code3");
                    if (selectSingleNode != null)
                        oMizanItems.Code3 = selectSingleNode.InnerText;
                    var singleNode = documentElement.SelectSingleNode("Serial3");
                    if (singleNode != null)
                        oMizanItems.Serial3 = singleNode.InnerText;
                    var node = documentElement.SelectSingleNode("Quantity3");
                    if (node != null)
                        oMizanItems.Quantity3 = node.InnerText;
                    var xmlNode = documentElement.SelectSingleNode("Fee3");
                    if (xmlNode != null)
                        oMizanItems.Fee3 = xmlNode.InnerText;
                    var selectSingleNode1 = documentElement.SelectSingleNode("Name3");
                    if (selectSingleNode1 != null)
                        oMizanItems.Name3 = selectSingleNode1.InnerText;
                }
                if (documentElement != null && documentElement.SelectSingleNode("Code4") != null)
                {
                    var selectSingleNode = documentElement.SelectSingleNode("Code4");
                    if (selectSingleNode != null)
                        oMizanItems.Code4 = selectSingleNode.InnerText;
                    var singleNode = documentElement.SelectSingleNode("Serial4");
                    if (singleNode != null)
                        oMizanItems.Serial4 = singleNode.InnerText;
                    var selectSingleNode1 = documentElement.SelectSingleNode("Quantity4");
                    if (selectSingleNode1 != null)
                        oMizanItems.Quantity4 = selectSingleNode1.InnerText;
                    var node = documentElement.SelectSingleNode("Fee4");
                    if (node != null)
                        oMizanItems.Fee4 = node.InnerText;
                    var xmlNode = documentElement.SelectSingleNode("Name4");
                    if (xmlNode != null)
                        oMizanItems.Name4 = xmlNode.InnerText;
                }
                if (documentElement != null && documentElement.SelectSingleNode("Code5") != null)
                {
                    oMizanItems.Code5 = documentElement.SelectSingleNode("Code5").InnerText;
                    oMizanItems.Serial5 = documentElement.SelectSingleNode("Serial5").InnerText;
                    oMizanItems.Quantity5 = documentElement.SelectSingleNode("Quantity5").InnerText;
                    var singleNode = documentElement.SelectSingleNode("Fee5");
                    if (singleNode != null)
                        oMizanItems.Fee5 = singleNode.InnerText;
                    var selectSingleNode = documentElement.SelectSingleNode("Name5");
                    if (selectSingleNode != null)
                        oMizanItems.Name5 = selectSingleNode.InnerText;
                }
                if (documentElement.SelectSingleNode("Code6") != null)
                {
                    oMizanItems.Code6 = documentElement.SelectSingleNode("Code6").InnerText;
                    oMizanItems.Serial6 = documentElement.SelectSingleNode("Serial6").InnerText;
                    oMizanItems.Quantity6 = documentElement.SelectSingleNode("Quantity6").InnerText;
                    oMizanItems.Fee6 = documentElement.SelectSingleNode("Fee6").InnerText;
                    oMizanItems.Name6 = documentElement.SelectSingleNode("Name6").InnerText;
                }
                if (documentElement.SelectSingleNode("Code7") != null)
                {
                    oMizanItems.Code7 = documentElement.SelectSingleNode("Code7").InnerText;
                    oMizanItems.Serial7 = documentElement.SelectSingleNode("Serial7").InnerText;
                    oMizanItems.Quantity7 = documentElement.SelectSingleNode("Quantity7").InnerText;
                    oMizanItems.Fee7 = documentElement.SelectSingleNode("Fee7").InnerText;
                    oMizanItems.Name7 = documentElement.SelectSingleNode("Name7").InnerText;
                }
                if (documentElement.SelectSingleNode("Code8") != null)
                {
                    oMizanItems.Code8 = documentElement.SelectSingleNode("Code8").InnerText;
                    oMizanItems.Serial8 = documentElement.SelectSingleNode("Serial8").InnerText;
                    oMizanItems.Quantity8 = documentElement.SelectSingleNode("Quantity8").InnerText;
                    oMizanItems.Fee8 = documentElement.SelectSingleNode("Fee8").InnerText;
                    oMizanItems.Name8 = documentElement.SelectSingleNode("Name8").InnerText;
                }
                if (documentElement.SelectSingleNode("Code9") != null)
                {
                    oMizanItems.Code9 = documentElement.SelectSingleNode("Code9").InnerText;
                    oMizanItems.Serial9 = documentElement.SelectSingleNode("Serial9").InnerText;
                    oMizanItems.Quantity9 = documentElement.SelectSingleNode("Quantity9").InnerText;
                    oMizanItems.Fee9 = documentElement.SelectSingleNode("Fee9").InnerText;
                    oMizanItems.Name9 = documentElement.SelectSingleNode("Name9").InnerText;
                }
                if (documentElement.SelectSingleNode("Code10") != null)
                {
                    oMizanItems.Code10 = documentElement.SelectSingleNode("Code10").InnerText;
                    oMizanItems.Serial10 = documentElement.SelectSingleNode("Serial10").InnerText;
                    oMizanItems.Quantity10 = documentElement.SelectSingleNode("Quantity10").InnerText;
                    oMizanItems.Fee10 = documentElement.SelectSingleNode("Fee10").InnerText;
                    oMizanItems.Name10 = documentElement.SelectSingleNode("Name10").InnerText;
                }
                oMizanItems.TotalFee = documentElement.SelectSingleNode("TotalFee").InnerText;
                oMizanItems.TaxFee = documentElement.SelectSingleNode("TaxFee").InnerText;
                oMizanItems.PrgVer = documentElement.SelectSingleNode("PrgVer").InnerText;
                oMizanItems.DllVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                this.InfoItems.Clear();
                this.PurchaseItems.Clear();
                if (!string.IsNullOrEmpty(oMizanItems.Code1))
                {
                    this.AddPurchaseItem(oMizanItems.Code1, oMizanItems.Serial1, oMizanItems.Quantity1, oMizanItems.Fee1,
                        oMizanItems.Name1);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code2))
                {
                    this.AddPurchaseItem(oMizanItems.Code2, oMizanItems.Serial2, oMizanItems.Quantity2, oMizanItems.Fee2,
                        oMizanItems.Name2);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code3))
                {
                    this.AddPurchaseItem(oMizanItems.Code3, oMizanItems.Serial3, oMizanItems.Quantity3, oMizanItems.Fee3,
                        oMizanItems.Name3);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code4))
                {
                    this.AddPurchaseItem(oMizanItems.Code4, oMizanItems.Serial4, oMizanItems.Quantity4, oMizanItems.Fee4,
                        oMizanItems.Name4);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code5))
                {
                    this.AddPurchaseItem(oMizanItems.Code5, oMizanItems.Serial5, oMizanItems.Quantity5, oMizanItems.Fee5,
                        oMizanItems.Name5);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code6))
                {
                    this.AddPurchaseItem(oMizanItems.Code6, oMizanItems.Serial6, oMizanItems.Quantity6, oMizanItems.Fee6,
                        oMizanItems.Name6);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code7))
                {
                    this.AddPurchaseItem(oMizanItems.Code7, oMizanItems.Serial7, oMizanItems.Quantity7, oMizanItems.Fee7,
                        oMizanItems.Name7);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code8))
                {
                    this.AddPurchaseItem(oMizanItems.Code8, oMizanItems.Serial8, oMizanItems.Quantity8, oMizanItems.Fee8,
                        oMizanItems.Name8);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code9))
                {
                    this.AddPurchaseItem(oMizanItems.Code9, oMizanItems.Serial9, oMizanItems.Quantity9, oMizanItems.Fee9,
                        oMizanItems.Name9);
                }
                if (!string.IsNullOrEmpty(oMizanItems.Code10))
                {
                    this.AddPurchaseItem(oMizanItems.Code10, oMizanItems.Serial10, oMizanItems.Quantity10,
                        oMizanItems.Fee10, oMizanItems.Name10);
                }
                this.AddInfoItem("TotalFee", oMizanItems.TotalFee);
                this.AddInfoItem("TaxFee", oMizanItems.TaxFee);
                this.AddInfoItem("DllVer", oMizanItems.DllVer);
                this.AddInfoItem("PrgVer", oMizanItems.PrgVer);
                this.CreateReciptPrintMizan(oMizanItems);
            }
            catch
            {
                this._xmlError = "خطا در  xml ورودی";
                return false;
            }
            this._ToTalAmount = Convert.ToInt64(oMizanItems.TotalFee);
            this._xmlError = string.Empty;
            return true;
        }

        // ReSharper disable once FunctionComplexityOverflow
        protected bool ParseInputXMLPublic(string xml)
        {
            var oPublicItems = new PublicItems();
            try
            {
                var document = new XmlDocument();
                try
                {
                    document.LoadXml(xml);
                }
                catch (XmlException exception)
                {
                    if (!exception.Message.Contains("There are multiple root elements."))
                    {
                        throw new XmlException("خطا در پردازش اطلاعات ورودی", exception);
                    }
                    //xml = string.Format("<ROOT>{0}</ROOT>", xml);
                    return false;
                }

                var documentElement = document.DocumentElement;
                var list = document.SelectNodes("//*[contains(name(),'Item')]");
                if (list != null)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var node = list[i];
                        var str = node.Name.Replace("Item", string.Empty);
                        if (string.Equals(this.Prg.ToLower(), "khazaneh"))
                        {
                            if (documentElement == null) continue;
                            var selectSingleNode = documentElement.SelectSingleNode("Amount" + str);
                            if (selectSingleNode != null)
                            {
                                var singleNode = documentElement.SelectSingleNode("Item" + str);
                                if (singleNode != null)
                                    oPublicItems.PItem.Add(new PItem(singleNode.InnerText,
                                        selectSingleNode.InnerText, "1"));
                            }
                        }
                        else
                        {
                            if (documentElement == null) continue;
                            var selectSingleNode = documentElement.SelectSingleNode("Printed" + str);
                            if (selectSingleNode == null) continue;
                            var singleNode = documentElement.SelectSingleNode("Item" + str);
                            if (singleNode == null) continue;
                            var xmlNode = documentElement.SelectSingleNode("Value" + str);
                            if (xmlNode != null)
                                oPublicItems.PItem.Add(new PItem(singleNode.InnerText,
                                    xmlNode.InnerText,
                                    selectSingleNode.InnerText));
                        }
                    }
                }

                if (documentElement != null)
                {
                    var singleNode = documentElement.SelectSingleNode("TotalFee");
                    if (singleNode != null)
                        oPublicItems.TotalFee = singleNode.InnerText;

                    try
                    {
                        var selectSingleNode = documentElement.SelectSingleNode("Amount1");
                        if (selectSingleNode != null)
                            oPublicItems.Amount1 = selectSingleNode.InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        var selectSingleNode = documentElement.SelectSingleNode("Amount2");
                        if (selectSingleNode != null)
                            oPublicItems.Amount2 = selectSingleNode.InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        var selectSingleNode = documentElement.SelectSingleNode("Amount3");
                        if (selectSingleNode != null)
                            oPublicItems.Amount3 = selectSingleNode.InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount4 = documentElement.SelectSingleNode("Amount4").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount5 = documentElement.SelectSingleNode("Amount5").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount6 = documentElement.SelectSingleNode("Amount6").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount7 = documentElement.SelectSingleNode("Amount7").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount8 = documentElement.SelectSingleNode("Amount8").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount9 = documentElement.SelectSingleNode("Amount9").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount10 = documentElement.SelectSingleNode("Amount10").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount11 = documentElement.SelectSingleNode("Amount11").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount12 = documentElement.SelectSingleNode("Amount12").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount13 = documentElement.SelectSingleNode("Amount13").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.Amount14 = documentElement.SelectSingleNode("Amount14").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        oPublicItems.TestFee = documentElement.SelectSingleNode("TestFee").InnerText;
                    }
                    catch
                    {
                        // ignored
                    }
                    oPublicItems.PrgVer = documentElement.SelectSingleNode("PrgVer").InnerText;
                }
                oPublicItems.DllVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                this.InfoItems.Clear();
                foreach (var item in oPublicItems.PItem)
                {
                    this.AddInfoItem(item.Code, item.Value);
                }
                this.AddInfoItem("DllVer", oPublicItems.DllVer);
                this.AddInfoItem("PrgVer", oPublicItems.PrgVer);
                this.CreateReciptPrintPublic(oPublicItems);
            }
            catch
            {
                this._xmlError = "خطا در  xml ورودی";
                return false;
            }
            this._ToTalAmount = Convert.ToInt64(oPublicItems.TotalFee);
            try
            {
                this._Amount1 = Convert.ToInt64(oPublicItems.Amount1);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount2 = Convert.ToInt64(oPublicItems.Amount2);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount3 = Convert.ToInt64(oPublicItems.Amount3);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount4 = Convert.ToInt64(oPublicItems.Amount4);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount5 = Convert.ToInt64(oPublicItems.Amount5);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount6 = Convert.ToInt64(oPublicItems.Amount6);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount7 = Convert.ToInt64(oPublicItems.Amount7);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount8 = Convert.ToInt64(oPublicItems.Amount8);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount9 = Convert.ToInt64(oPublicItems.Amount9);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount10 = Convert.ToInt64(oPublicItems.Amount10);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount11 = Convert.ToInt64(oPublicItems.Amount11);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount12 = Convert.ToInt64(oPublicItems.Amount12);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount13 = Convert.ToInt64(oPublicItems.Amount13);
            }
            catch
            {
                // ignored
            }
            try
            {
                this._Amount14 = Convert.ToInt64(oPublicItems.Amount14);
            }
            catch
            {
                // ignored
            }
            this._xmlError = string.Empty;
            return true;
        }

        // ReSharper disable once FunctionComplexityOverflow
        protected bool ParseInputXMLSabt(string xml)
        {
            var oSabtItems = new SabtItems();
            try
            {
                var document = new XmlDocument();
                document.LoadXml(string.Format("<test>{0}</test>", xml));
                var documentElement = document.DocumentElement;
                if (documentElement != null)
                {
                    var node = documentElement.SelectSingleNode("TransactionType");
                    if (node == null)
                    {
                        this._xmlError = "اشکال در ساختار xml";
                        return false;
                    }
                    if (node.InnerText == "Document")
                    {
                        var selectSingleNode = documentElement.SelectSingleNode("VendorName");
                        if (selectSingleNode != null)
                            oSabtItems.VendorName = selectSingleNode.InnerText;
                        var singleNode = documentElement.SelectSingleNode("VendorInternationalCode");
                        if (singleNode != null)
                            oSabtItems.VendorNationalCode =
                                singleNode.InnerText;
                        var node1 = documentElement.SelectSingleNode("VendeeName");
                        if (node1 != null)
                            oSabtItems.VendeeName = node1.InnerText;
                        var xmlNode2 = documentElement.SelectSingleNode("VendeeInternationalCode");
                        if (xmlNode2 != null)
                            oSabtItems.VendeeNationalCode =
                                xmlNode2.InnerText;
                        var singleNode2 = documentElement.SelectSingleNode("DocumentNumber");
                        if (singleNode2 != null)
                            oSabtItems.DocumentNumber = singleNode2.InnerText;
                        var selectSingleNode2 = documentElement.SelectSingleNode("DocumentTypeCode");
                        if (selectSingleNode2 != null)
                            oSabtItems.DocumentType = selectSingleNode2.InnerText;
                        var selectSingleNode4 = documentElement.SelectSingleNode("DocumentTypeNameLevel1");
                        if (selectSingleNode4 != null)
                            oSabtItems.DocumentTypeNameLevel1 =
                                selectSingleNode4.InnerText;
                        var node3 = documentElement.SelectSingleNode("DocumentTypeNameLevel2");
                        if (node3 != null)
                            oSabtItems.DocumentTypeNameLevel2 =
                                node3.InnerText;
                        var selectSingleNode3 = documentElement.SelectSingleNode("DocumentFee");
                        if (selectSingleNode3 != null)
                            oSabtItems.DocumentFee = selectSingleNode3.InnerText;
                        var xmlNode3 = documentElement.SelectSingleNode("ElectronicIssuance");
                        if (xmlNode3 != null)
                            oSabtItems.ElectronicIssuance = xmlNode3.InnerText;
                        var singleNode3 = documentElement.SelectSingleNode("RegistrationFee");
                        if (singleNode3 != null)
                            oSabtItems.RegistrationFee = singleNode3.InnerText;
                        var node2 = documentElement.SelectSingleNode("TaxCar");
                        if (node2 != null)
                            oSabtItems.TaxCar = node2.InnerText;
                        var xmlNode = documentElement.SelectSingleNode("NotaryFees");
                        if (xmlNode != null)
                            oSabtItems.NotaryFees = xmlNode.InnerText;
                        var selectSingleNode1 = documentElement.SelectSingleNode("TotalFee");
                        if (selectSingleNode1 != null)
                            oSabtItems.TotalFee = selectSingleNode1.InnerText;
                        var singleNode1 = documentElement.SelectSingleNode("ReferenceNumber");
                        if (singleNode1 != null)
                            oSabtItems.ReferenceNumber = singleNode1.InnerText;
                        var xmlNode1 = documentElement.SelectSingleNode("PrgVer");
                        if (xmlNode1 != null)
                            oSabtItems.PrgVer = xmlNode1.InnerText;
                        oSabtItems.DllVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                        this.InfoItems.Clear();
                        this.AddInfoItem("VendorName", oSabtItems.VendorName);
                        this.AddInfoItem("VendorNationalCode", oSabtItems.VendorNationalCode);
                        this.AddInfoItem("VendeeName", oSabtItems.VendeeName);
                        this.AddInfoItem("VendeeNationalCode", oSabtItems.VendeeNationalCode);
                        this.AddInfoItem("DocumentNumber", oSabtItems.DocumentNumber);
                        this.AddInfoItem("DocumentFee", oSabtItems.DocumentFee);
                        this.AddInfoItem("ElectronicIssuance", oSabtItems.ElectronicIssuance);
                        this.AddInfoItem("DocumentType", oSabtItems.DocumentType);
                        this.AddInfoItem("ReferenceNumber", oSabtItems.ReferenceNumber);
                        this.AddInfoItem("DllVer", oSabtItems.DllVer);
                        this.AddInfoItem("PrgVer", oSabtItems.PrgVer);
                        this.CreateReciptPrintSabt(1, oSabtItems);
                    }
                    else if (node.InnerText == "Service")
                    {
                        var xmlNode = documentElement.SelectSingleNode("ServiceNumber");
                        if (xmlNode != null)
                            oSabtItems.ServiceNumber = xmlNode.InnerText;
                        var selectSingleNode1 = documentElement.SelectSingleNode("ServiceTypeCode");
                        if (selectSingleNode1 != null)
                            oSabtItems.ServiceTypeCode = selectSingleNode1.InnerText;
                        var singleNode1 = documentElement.SelectSingleNode("ServiceTypeName");
                        if (singleNode1 != null)
                            oSabtItems.ServiceTypeName = singleNode1.InnerText;
                        if (documentElement.SelectSingleNode("StartSignWitness") != null)
                        {
                            var selectSingleNode = documentElement.SelectSingleNode("StartSignWitness");
                            if (selectSingleNode != null)
                                oSabtItems.StartSignWitness = selectSingleNode.InnerText;
                        }
                        if (documentElement.SelectSingleNode("EndSignWitness") != null)
                        {
                            var selectSingleNode = documentElement.SelectSingleNode("EndSignWitness");
                            if (selectSingleNode != null)
                                oSabtItems.EndSignWitness = selectSingleNode.InnerText;
                        }
                        var singleNode = documentElement.SelectSingleNode("ServiceFee");
                        if (singleNode != null)
                            oSabtItems.ServiceFee = singleNode.InnerText;
                        var xmlNode1 = documentElement.SelectSingleNode("TotalFee");
                        if (xmlNode1 != null)
                            oSabtItems.TotalFee = xmlNode1.InnerText;
                        var node1 = documentElement.SelectSingleNode("NotaryFees");
                        if (node1 != null)
                            oSabtItems.NotaryFees = node1.InnerText;
                        var selectSingleNode2 = documentElement.SelectSingleNode("OtherRegistrationFee");
                        if (selectSingleNode2 != null)
                            oSabtItems.OtherRegistrationFee =
                                selectSingleNode2.InnerText;
                        var singleNode2 = documentElement.SelectSingleNode("ReferenceNumber");
                        if (singleNode2 != null)
                            oSabtItems.ReferenceNumber = singleNode2.InnerText;
                        var xmlNode2 = documentElement.SelectSingleNode("PrgVer");
                        if (xmlNode2 != null)
                            oSabtItems.PrgVer = xmlNode2.InnerText;
                        oSabtItems.DllVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                        this.InfoItems.Clear();
                        this.AddInfoItem("ServiceNumber", oSabtItems.ServiceNumber);
                        this.AddInfoItem("ServiceFee", oSabtItems.ServiceFee);
                        this.AddInfoItem("ServiceType", oSabtItems.ServiceTypeCode);
                        this.AddInfoItem("ServiceTypeName", oSabtItems.ServiceTypeName);
                        if (!string.IsNullOrEmpty(oSabtItems.StartSignWitness))
                        {
                            this.AddInfoItem("StartSignWitness", oSabtItems.StartSignWitness);
                        }
                        else
                        {
                            this.AddInfoItem("StartSignWitness", "0");
                        }
                        if (!string.IsNullOrEmpty(oSabtItems.EndSignWitness))
                        {
                            this.AddInfoItem("EndSignWitness", oSabtItems.EndSignWitness);
                        }
                        else
                        {
                            this.AddInfoItem("EndSignWitness", "0");
                        }
                        this.AddInfoItem("ReferenceNumber", oSabtItems.ReferenceNumber);
                        this.AddInfoItem("DllVer", oSabtItems.DllVer);
                        this.AddInfoItem("PrgVer", oSabtItems.PrgVer);
                        if (oSabtItems.ServiceTypeCode == "207")
                        {
                            this.CreateReciptPrintSabt(3, oSabtItems);
                        }
                        else
                        {
                            this.CreateReciptPrintSabt(2, oSabtItems);
                        }
                    }
                    else if (node.InnerText == "Inquiry")
                    {
                        var xmlNode = documentElement.SelectSingleNode("ServiceNumber");
                        if (xmlNode != null)
                            oSabtItems.ServiceNumber = xmlNode.InnerText;
                        var selectSingleNode = documentElement.SelectSingleNode("VendorName");
                        if (selectSingleNode != null)
                            oSabtItems.VendorName = selectSingleNode.InnerText;
                        var singleNode = documentElement.SelectSingleNode("VendorInternationalCode");
                        if (singleNode != null)
                            oSabtItems.VendorNationalCode =
                                singleNode.InnerText;
                        var selectSingleNode1 = documentElement.SelectSingleNode("ServiceTypeCode");
                        if (selectSingleNode1 != null)
                            oSabtItems.ServiceTypeCode = selectSingleNode1.InnerText;
                        var xmlNode3 = documentElement.SelectSingleNode("ServiceTypeName");
                        if (xmlNode3 != null)
                            oSabtItems.ServiceTypeName = xmlNode3.InnerText;
                        var singleNode3 = documentElement.SelectSingleNode("ServiceFee");
                        if (singleNode3 != null)
                            oSabtItems.ServiceFee = singleNode3.InnerText;
                        var selectSingleNode3 = documentElement.SelectSingleNode("InquiryDocNo");
                        if (selectSingleNode3 != null)
                            oSabtItems.InquiryDocNo = selectSingleNode3.InnerText;
                        var node2 = documentElement.SelectSingleNode("StateID");
                        if (node2 != null)
                            oSabtItems.StateID = node2.InnerText;
                        var xmlNode2 = documentElement.SelectSingleNode("ZoneID");
                        if (xmlNode2 != null)
                            oSabtItems.ZoneID = xmlNode2.InnerText;
                        var singleNode2 = documentElement.SelectSingleNode("TotalFee");
                        if (singleNode2 != null)
                            oSabtItems.TotalFee = singleNode2.InnerText;
                        var selectSingleNode2 = documentElement.SelectSingleNode("NotaryFees");
                        if (selectSingleNode2 != null)
                            oSabtItems.NotaryFees = selectSingleNode2.InnerText;
                        var node1 = documentElement.SelectSingleNode("InquiryFee");
                        if (node1 != null)
                            oSabtItems.InquiryFee = node1.InnerText;
                        var xmlNode1 = documentElement.SelectSingleNode("ReferenceNumber");
                        if (xmlNode1 != null)
                            oSabtItems.ReferenceNumber = xmlNode1.InnerText;
                        var singleNode1 = documentElement.SelectSingleNode("PrgVer");
                        if (singleNode1 != null)
                            oSabtItems.PrgVer = singleNode1.InnerText;
                        oSabtItems.DllVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                        this.InfoItems.Clear();
                        this.AddInfoItem("VendorName", oSabtItems.VendorName);
                        this.AddInfoItem("VendorNationalCode", oSabtItems.VendorNationalCode);
                        this.AddInfoItem("ServiceNumber", oSabtItems.ServiceNumber);
                        this.AddInfoItem("ServiceFee", oSabtItems.ServiceFee);
                        this.AddInfoItem("ServiceType", oSabtItems.ServiceTypeCode);
                        this.AddInfoItem("ServiceTypeName", oSabtItems.ServiceTypeName);
                        this.AddInfoItem("InquiryDocNo", oSabtItems.InquiryDocNo);
                        this.AddInfoItem("StateID", oSabtItems.StateID);
                        this.AddInfoItem("ZoneID", oSabtItems.ZoneID);
                        this.AddInfoItem("DllVer", oSabtItems.DllVer);
                        this.AddInfoItem("PrgVer", oSabtItems.PrgVer);
                        this.CreateReciptPrintSabt(4, oSabtItems);
                    }
                    else if (node.InnerText == "Remaining")
                    {
                        var node1 = documentElement.SelectSingleNode("ServiceTypeCode");
                        if (node1 != null)
                            oSabtItems.ServiceTypeCode = node1.InnerText;
                        var xmlNode1 = documentElement.SelectSingleNode("ServiceTypeName");
                        if (xmlNode1 != null)
                            oSabtItems.ServiceTypeName = xmlNode1.InnerText;
                        var singleNode1 = documentElement.SelectSingleNode("TotalFee");
                        if (singleNode1 != null)
                            oSabtItems.TotalFee = singleNode1.InnerText;
                        var selectSingleNode1 = documentElement.SelectSingleNode("NotaryFees");
                        if (selectSingleNode1 != null)
                            oSabtItems.NotaryFees = selectSingleNode1.InnerText;
                        var xmlNode = documentElement.SelectSingleNode("RemainingFee");
                        if (xmlNode != null)
                            oSabtItems.RemainingFee = xmlNode.InnerText;
                        var singleNode = documentElement.SelectSingleNode("ReferenceNumber");
                        if (singleNode != null)
                            oSabtItems.ReferenceNumber = singleNode.InnerText;
                        var selectSingleNode = documentElement.SelectSingleNode("PrgVer");
                        if (selectSingleNode != null)
                            oSabtItems.PrgVer = selectSingleNode.InnerText;
                        oSabtItems.DllVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                        this.InfoItems.Clear();
                        this.AddInfoItem("ServiceType", oSabtItems.ServiceTypeCode);
                        this.AddInfoItem("ReferenceNumber", oSabtItems.ReferenceNumber);
                        this.AddInfoItem("DllVer", oSabtItems.DllVer);
                        this.AddInfoItem("PrgVer", oSabtItems.PrgVer);
                        this.CreateReciptPrintSabt(5, oSabtItems);
                    }
                    else if (node.InnerText == "OnePage")
                    {
                        var selectSingleNode = documentElement.SelectSingleNode("ServiceTypeCode");
                        if (selectSingleNode != null)
                            oSabtItems.ServiceTypeCode = selectSingleNode.InnerText;
                        var singleNode = documentElement.SelectSingleNode("ServiceTypeName");
                        if (singleNode != null)
                            oSabtItems.ServiceTypeName = singleNode.InnerText;
                        var xmlNode = documentElement.SelectSingleNode("NotaryFees");
                        if (xmlNode != null)
                            oSabtItems.NotaryFees = xmlNode.InnerText;
                        var selectSingleNode1 = documentElement.SelectSingleNode("ChangingFee");
                        if (selectSingleNode1 != null)
                            oSabtItems.ChangingFee = selectSingleNode1.InnerText;
                        var singleNode2 = documentElement.SelectSingleNode("PagesPriceFee");
                        if (singleNode2 != null)
                            oSabtItems.PagesPriceFee = singleNode2.InnerText;
                        var singleNode1 = documentElement.SelectSingleNode("RegistrationFee");
                        if (singleNode1 != null)
                            oSabtItems.RegistrationFee = singleNode1.InnerText;
                        var selectSingleNode2 = documentElement.SelectSingleNode("ReferenceNumber");
                        if (selectSingleNode2 != null)
                            oSabtItems.ReferenceNumber = selectSingleNode2.InnerText;
                        var xmlNode1 = documentElement.SelectSingleNode("TotalFee");
                        if (xmlNode1 != null)
                            oSabtItems.TotalFee = xmlNode1.InnerText;
                        var node1 = documentElement.SelectSingleNode("PrgVer");
                        if (node1 != null)
                            oSabtItems.PrgVer = node1.InnerText;
                        oSabtItems.DllVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                        this.InfoItems.Clear();
                        this.AddInfoItem("ServiceType", oSabtItems.ServiceTypeCode);
                        this.AddInfoItem("ReferenceNumber", oSabtItems.ReferenceNumber);
                        this.AddInfoItem("DllVer", oSabtItems.DllVer);
                        this.AddInfoItem("PrgVer", oSabtItems.PrgVer);
                        this.CreateReciptPrintSabt(6, oSabtItems);
                    }
                    else if (node.InnerText == "Test")
                    {
                        var selectSingleNode1 = documentElement.SelectSingleNode("TestFee");
                        if (selectSingleNode1 != null)
                            oSabtItems.TestFee = selectSingleNode1.InnerText;
                        var xmlNode = documentElement.SelectSingleNode("TestFee");
                        if (xmlNode != null)
                            oSabtItems.TotalFee = xmlNode.InnerText;
                        var singleNode = documentElement.SelectSingleNode("ReferenceNumber");
                        if (singleNode != null)
                            oSabtItems.ReferenceNumber = singleNode.InnerText;
                        var selectSingleNode = documentElement.SelectSingleNode("PrgVer");
                        if (selectSingleNode != null)
                            oSabtItems.PrgVer = selectSingleNode.InnerText;
                        oSabtItems.DllVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                        this.InfoItems.Clear();
                        this.AddInfoItem("ReferenceNumber", oSabtItems.ReferenceNumber);
                        this.AddInfoItem("DllVer", oSabtItems.DllVer);
                        this.AddInfoItem("PrgVer", oSabtItems.PrgVer);
                        this.CreateReciptPrintSabt(7, oSabtItems);
                    }
                    else
                    {
                        this._xmlError = "خطا در  xml ورودی";
                        return false;
                    }
                }
            }
            catch
            {
                this._xmlError = "خطا در  xml ورودی";
                return false;
            }
            this._Amount1 = Convert.ToInt64(oSabtItems.RegistrationFee);
            this._Amount2 = Convert.ToInt64(oSabtItems.TaxCar);
            this._Amount3 = Convert.ToInt64(oSabtItems.OtherRegistrationFee);
            this._Amount4 = Convert.ToInt64(oSabtItems.InquiryFee);
            this._Amount5 = Convert.ToInt64(oSabtItems.RemainingFee);
            this._Amount6 = Convert.ToInt64(oSabtItems.ChangingFee);
            if (!string.IsNullOrEmpty(oSabtItems.ElectronicIssuance))
            {
                this._Amount7 = Convert.ToInt64(oSabtItems.ElectronicIssuance);
            }
            this._Amount8 = Convert.ToInt64(oSabtItems.NotaryFees);
            this._AmountTest = Convert.ToInt64(oSabtItems.TestFee);
            this._ToTalAmount = Convert.ToInt64(oSabtItems.TotalFee);
            this._xmlError = string.Empty;
            return true;
        }

        public bool Send(string xml, byte kind = 0)
        {
            return kind == 0 ? this.SendToSocket(xml) : this.SendToCOM(xml);
        }

        public bool SendToCOM(string xml)
        {
            try
            {
                if (this.InitMessage(xml))
                {
                    comPort.DiscardInBuffer();
                    comPort.DiscardOutBuffer();
                    comPort.Write(this.finalMessage, 0, this.finalMessage.Length);
                    return true;
                }
                this._xmlError = "خطا در ارسال اطلاعات";
                return false;
            }
            catch (Exception)
            {
                this._xmlError = "خطا در ارسال اطلاعات";
                return false;
            }
        }

        public bool SendToSocket(string xml)
        {
            try
            {
                if (this.InitMessage(xml))
                {
                    if (this.client != null)
                    {
                        NetworkStream stream = this.client.GetStream();
                        stream.Write(this.finalMessage, 0, this.finalMessage.Length);
                        stream.Flush();
                    }
                    this._xmlError = string.Empty;
                    return true;
                }
                this._xmlError = "خطا در ارسال اطلاعات";
                return false;
            }
            catch
            {
                this._xmlError = "خطا در ارسال اطلاعات";
                return false;
            }
        }

        public void SetConfirmFlag(bool i)
        {
            this._confirmFlag = i;
        }

        public void SetPort(string PortName)
        {
            this.Port = PortName;
        }

        public void SetPrintFlag(byte i)
        {
            this._printFlag = i;
        }

        private void ShowReceiveMessage()
        {
            switch (this.RetResponseCode)
            {
                case "0":
                    this.ReturnedMessage = "عملیات پرداخت با موفقیت انجام گردید";
                    break;

                case "1":
                    this.ReturnedMessage = "عدم دریافت پاسخ در زمان مناسب";
                    break;

                case "2":
                    this.ReturnedMessage = "داده نامعتبر";
                    break;

                case "3":
                    this.ReturnedMessage = "کنسل شدن عملیات توسط مشتری";
                    break;

                case "4":
                case "5":
                    this.ReturnedMessage = "عدم ارتباط با مرکز";
                    break;

                case "19":
                    this.ReturnedMessage = "تراکنش را مجدد تکرار کنيد";
                    break;

                case "100":
                case "51":
                    this.ReturnedMessage = "موجودی شما کافی نیست";
                    break;

                case "101":
                case "55":
                    this.ReturnedMessage = "رمز کارت اشتباه است";
                    break;

                case "57":
                    this.ReturnedMessage = "دارنده کارت مجوز انجام چنين تراکنشي را ندارد";
                    break;

                case "61":
                    this.ReturnedMessage = "سقف مبلغ تراکنش برداشت وجه رعايت نشده است";
                    break;

                case "63":
                    this.ReturnedMessage = "خطای امنیتی";
                    break;

                case "68":
                    this.ReturnedMessage = "پاسخ در زمان مناسب از مرکز دریافت نشد";
                    break;

                case "75":
                    this.ReturnedMessage = "دفعات ورود رمز اشتباه بیشتر از حد مجاز";
                    break;

                case "78":
                    this.ReturnedMessage = "کارت غير فعال شده است";
                    break;

                case "84":
                    this.ReturnedMessage = "خطای صادر کننده";
                    break;

                case "90":
                    this.ReturnedMessage = "در حال تغيير دوره مالي";
                    break;

                case "94":
                    this.ReturnedMessage = "ارسال تکراري تراکنش بوجود آمده است";
                    break;

                default:
                    this.ReturnedMessage = "خطای نامشخص";
                    break;
            }
            if (this.ShowMessages)
            {
                MessageBox.Show(this.ReturnedMessage);
            }
            string retRRN = this.RetRRN;
            if (this._prg == "sabt")
            {
                retRRN = Myfunc.SabtCheckDigit(this.RetBatchNo, this.RetSerialNo);
            }
            this._xmlRecieve = "<TerminalId>" + this.RetTermID + "</TerminalId><SerialNumber>" + this.RetSerialNo +
                               "</SerialNumber><BatchNumber>" + this.RetBatchNo + "</BatchNumber><CustomerPan>" +
                               this.RetPAN + "</CustomerPan><DateTime>" + this.RetDateTime + "</DateTime><ResponseCode>" +
                               this.RetResponseCode + "</ResponseCode><ReferenceNumber>" + retRRN +
                               "</ReferenceNumber><TraceNumber>" + this.RetTraceNo + "</TraceNumber><Amount>" +
                               this.RetAmount + "</Amount><AffectedAmount>" + this.RetAffectedAmount +
                               "</AffectedAmount><ConfirmTransaction>Yes</ConfirmTransaction>";
            this.XmlRecieveState = "XMLRecive";
        }

        // ReSharper disable once ConvertToAutoProperty
        public static SerialPort comPort
        {
            get { return __comPort; }
            set { __comPort = value; }
        }

        public bool ConfirmFlag
        {
            get { return this._confirmFlag; }
            set { this._confirmFlag = value; }
        }

        public bool IsPortOpen
        {
            get { return this._isPortOpen; }
        }

        public string Port
        {
            get
            {
                if (comPort == null)
                {
                    return string.Empty;
                }
                return comPort.PortName;
            }
            set
            {
                if (comPort == null)
                {
                    this.Init();
                }

                if (comPort != null)
                {
                    comPort.PortName = value;
                }
            }
        }

        public string Prg
        {
            get
            {
                if (!string.IsNullOrEmpty(this._prg)) return this._prg;

#pragma warning disable 618
                var str = ConfigurationSettings.AppSettings.Get("prg");
#pragma warning restore 618

                if (!string.IsNullOrEmpty(str))
                {
                    this._prg = str;
                }
                else
                {
                    this._prg = "public";
                }
                return this._prg;
            }
            set { this._prg = value; }
        }

        public byte PrintFlag
        {
            get { return this._printFlag; }
            set { this._printFlag = value; }
        }

        public string receivdText
        {
            get { return this._receivdText; }
            set { this._receivdText = value; }
        }

        public string ReturnedMessage { get; set; }

        public bool ShowMessages { get; set; }

        public string XmlError
        {
            get { return this._xmlError; }
        }

        public string XmlRecieve
        {
            get { return this._xmlRecieve; }
        }

        public string XmlRecieveState
        {
            get { return this._xmlRecieveState; }
            private set
            {
                if (this._xmlRecieveState != value)
                {
                    this._xmlRecieveState = value;
                    if ((this.XMLReceived != null) && (this.XmlRecieveState == "XMLRecive"))
                    {
                        this.XMLReceived(this,
                            new XmlReceivedEventArgs(this.RetResponseCode == "0", this.XmlRecieve,
                                new PosResponse(this.RetTermID, this.RetSerialNo, this.RetBatchNo, this.RetPAN,
                                    this.RetDateTime, this.RetResponseCode, this.RetRRN, this.RetTraceNo,
                                    this.ReturnedMessage, this.RetAmount, this.RetAffectedAmount, true)));
                    }
                }
            }
        }
    }
}

