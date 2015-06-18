using System;
using System.Collections.Generic;
using System.IO;
using SamanPcToPos.Helper;
using SamanPcToPos.Helper.Exceptions;
using SamanPcToPos.ISOHelper.Util;

namespace SamanPcToPos.TLV
{
    public class TLVMsg
    {
        private readonly List<TLVMsg> children;
        private readonly string comment;
        private int tag;
        private byte[] value;

        public TLVMsg()
        {
            this.comment = "";
            this.children = new List<TLVMsg>();
        }

        public TLVMsg(int tag1, byte[] value1, string cmt)
        {
            this.comment = "";
            this.children = new List<TLVMsg>();
            this.tag = tag1;
            this.value = value1;
            this.comment = cmt;
        }

        public void AddChild(TLVMsg msg)
        {
            this.children.Add(msg);
        }

        // ReSharper disable once ParameterHidesMember
        public TLVMsg AddChild(int tag, byte[] value, string cmnt)
        {
            var item = new TLVMsg(tag, value, cmnt);
            this.children.Add(item);
            return item;
        }

        public int CalcLength()
        {
            if ((this.value == null) && (this.children.Count == 0))
            {
                return 0;
            }
            var num = (this.value != null) ? this.value.Length : 0;
            if (this.children.Count > 0)
            {
                var num2 = 0;
                foreach (var msg in this.children)
                {
                    int num3 = msg.CalcLength();
                    if (num3 != 0)
                    {
                        num2 += num3;
                    }
                }
                num = num2;
            }
            if (num > 0x7f)
            {
                return (num + (2 + ((num > 0x7f) ? ((int) Math.Ceiling(Math.Log(num, 2.0) / 8.0)) : 0)));
            }
            return (num + 2);
        }

        public void dump(StreamWriter p, string indent)
        {
            p.WriteLine(indent + "<" + this.comment.Trim() + "Id_" + string.Format("0x{0:X}", this.tag) + " Length=" + string.Format("0x{0:X}", this.CalcLength()) + ">");
            if (this.children.Count == 0)
            {
                if (this.value != null)
                {
                    p.WriteLine(indent + "   " + ISOUtil.hexString(this.value));
                }
                else
                {
                    p.WriteLine(indent + "   ");
                }
            }
            else
            {
                foreach (var msg in this.children)
                {
                    msg.dump(p, indent + "   ");
                }
            }
            p.WriteLine(indent + "</" + this.comment.Trim() + "Id_" + string.Format("0x{0:X}", this.tag) + ">");
            p.Flush();
        }

        public byte[] finalize()
        {
            byte[] buffer = this.getTLV();
            byte[] buffer2 = new byte[buffer.Length + 6];
            buffer2[0] = 2;
            buffer2[1] = (byte) buffer.Length;
            buffer2[2] = 0;
            buffer2[3] = 0;
            buffer2[4] = 0;
            buffer2[5] = 1;
            for (var i = 6; i < buffer2.Length; i++)
            {
                buffer2[i] = buffer[i - 6];
            }
            return buffer2;
        }

        // ReSharper disable once ParameterHidesMember
        public TLVMsg getChild(int tag, int i)
        {
            if (this.children == null) return null;
            foreach (var msg in this.children)
            {
                if (msg.tag != tag) continue;
                i--;
                if (i <= 0)
                {
                    return msg;
                }
            }
            return null;
        }

        public byte[] getL()
        {
            byte[] buffer;
            if ((this.value == null) && (this.children.Count == 0))
            {
                return new byte[1];
            }
            var num = this.CalcLength();
            num -= 2 + (((num - 2) > 0x7f) ? ((int) Math.Ceiling(Math.Log(num - 2, 2.0) / 8.0)) : 0);
            var num2 = num;
            const int num3 = 0;
            var num4 = 0;
            while (num != 0)
            {
                num = num >> 8;
                num4++;
            }
            if ((num4 >= 1) && (num2 <= 0x7f))
            {
                buffer = new byte[num4];
                buffer[0] = (byte) num2;
                return buffer;
            }
            buffer = new byte[1 + num4];
            buffer[0] = (byte) (0x80 | num4);
            const int num5 = 0xff;
            num = num2;
            for (byte i = 0; num3 < num4; i = (byte) (i + 1))
            {
                buffer[num4 - num3] = (byte) (num & num5);
                num4--;
                num = num >> 8;
            }
            return buffer;
        }

        public string getStringValue()
        {
            return ISOUtil.hexString(this.value);
        }

        public int getTag()
        {
            return this.tag;
        }

        private int getTAG(Stream buffer)
        {
            var num = Convert.ToByte(buffer.ReadByte()) & 0xff;
            switch (num)
            {
                case 0xff:
                case 0:
                    do
                    {
                        num = Convert.ToByte(buffer.ReadByte()) & 0xff;
                    }
                    while (((num == 0xff) || (num == 0)) && (buffer.Position < buffer.Length));
                    break;
            }
            var num2 = num;
            if ((num & 0x1f) != 0x1f) return num2;
            do
            {
                num2 = num2 << 8;
                num = Convert.ToByte(buffer.ReadByte());
                num2 |= num & 0xff;
            }
            while ((num & 0x80) == 0x80);
            return num2;
        }

        public byte[] getTLV()
        {
            byte[] sourceArray = ISOUtil.hex2byte(string.Format("{0:X}", this.tag));
            byte[] buffer2 = this.getL();
            if ((this.value != null) && (this.children.Count == 0))
            {
                int num = (sourceArray.Length + buffer2.Length) + this.value.Length;
                byte[] buffer3 = new byte[num];
                Array.Copy(sourceArray, 0, buffer3, 0, sourceArray.Length);
                Array.Copy(buffer2, 0, buffer3, sourceArray.Length, buffer2.Length);
                Array.Copy(this.value, 0, buffer3, sourceArray.Length + buffer2.Length, this.value.Length);
                return buffer3;
            }
            if (this.children.Count != 0)
            {
                int num2 = sourceArray.Length + buffer2.Length;
                byte[] buffer4 = new byte[(this.CalcLength() + num2) + 100];
                int destinationIndex = 0;
                Array.Copy(sourceArray, 0, buffer4, 0, sourceArray.Length);
                Array.Copy(buffer2, 0, buffer4, sourceArray.Length, buffer2.Length);
                destinationIndex += num2;
                foreach (TLVMsg msg in this.children)
                {
                    byte[] buffer5 = msg.getTLV();
                    Array.Copy(buffer5, 0, buffer4, destinationIndex, buffer5.Length);
                    destinationIndex += buffer5.Length;
                }
                byte[] buffer6 = new byte[destinationIndex];
                Array.Copy(buffer4, 0, buffer6, 0, destinationIndex);
                return buffer6;
            }
            var num4 = sourceArray.Length + buffer2.Length;
            var destinationArray = new byte[num4];
            Array.Copy(sourceArray, 0, destinationArray, 0, sourceArray.Length);
            Array.Copy(buffer2, 0, destinationArray, sourceArray.Length, buffer2.Length);
            return destinationArray;
        }

        private TLVMsg getTLVMsg(MemoryStream buffer)
        {
            try
            {
                int[] ar = { 0x72, 0xb1, 0xb2, 0xa1, 0xa2 };
                var num = this.getTAG(buffer);
                if (num == 0) return null;
                byte[] buffer2 = null;
                if (buffer.Position >= buffer.Length)
                {
                    throw new ISOException("BAD TLV FORMAT - tag (" + string.Format("{0:x}", num) + ") without length or value");
                }
                var count = this.getValueLength(buffer);
                if (count > (buffer.Length - buffer.Position))
                {
                    throw new ISOException(string.Concat("BAD TLV FORMAT - tag (", string.Format("{0:x}", num), ") length (", count, ") exceeds available data."));
                }
                if (!Myfunc.InArray(ar, num))
                {
                    if (count > 0)
                    {
                        buffer2 = new byte[count];
                        buffer.Read(buffer2, 0, count);
                    }
                    return new TLVMsg(num, buffer2, "");
                }
                var msg = new TLVMsg {
                    tag = num
                };
                TLVMsg msg2;
                var position = buffer.Position;
                while ((msg2 = this.getTLVMsg(buffer)) != null)
                {
                    msg.AddChild(msg2);
                    if ((buffer.Position - position) >= count)
                    {
                        return msg;
                    }
                }
                return msg;
            }
            catch
            {
                return null;
            }
        }

        public byte[] getValue()
        {
            return this.value;
        }

        private int getValueLength(MemoryStream buffer)
        {
            var num2 = Convert.ToByte(buffer.ReadByte()) & 0xff;
            if ((num2 & 0x80) == 0x80)
            {
                num2 -= 0x80;
                var num = 0;
                while (num2 > 0)
                {
                    num = num << 8;
                    var num3 = Convert.ToByte(buffer.ReadByte());
                    num |= num3 & 0xff;
                    num2--;
                }
                return num;
            }
            return num2;
        }

        // ReSharper disable once ParameterHidesMember
        public void setTag(int tag)
        {
            this.tag = tag;
        }

        public void setValue(byte[] newValue)
        {
            this.value = newValue;
        }

        public void unpack(byte[] buf)
        {
            var buffer = new MemoryStream(buf);
            var msg = this.getTLVMsg(buffer);
            if (msg != null)
            {
                this.AddChild(msg);
            }
        }
    }
}

