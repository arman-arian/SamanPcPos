using System;
using System.Collections;
using System.IO;
using SamanPcToPos.Helper.Exceptions;

namespace SamanPcToPos.TLV
{
    public class TLVList
    {
        private int indexLastOccurrence = -1;
        private readonly ArrayList tags;
        private int tagToFind;

        public TLVList()
        {
            tags = new ArrayList();
        }

        public void append(TLVMsg tlvToAppend)
        {
            this.tags.Add(tlvToAppend);
        }

        public void append(int tag, byte[] value)
        {
            this.append(new TLVMsg(tag, value, ""));
        }

        public void deleteByIndex(int index)
        {
            this.tags.Remove(index);
        }

        public void deleteByTag(int tag)
        {
            int num = 0;
            while (num < this.tags.Count)
            {
                TLVMsg msg = (TLVMsg) this.tags[num];
                if (msg.getTag() == tag)
                {
                    this.tags.Remove(msg);
                }
                else
                {
                    num++;
                }
            }
        }

        public IEnumerator elements()
        {
            return this.tags.GetEnumerator();
        }

        public TLVMsg find(int tag)
        {
            int num = 0;
            this.tagToFind = tag;
            while (num < this.tags.Count)
            {
                TLVMsg msg = (TLVMsg) this.tags[num];
                if (msg.getTag() == tag)
                {
                    this.indexLastOccurrence = num;
                    return msg;
                }
                num++;
            }
            this.indexLastOccurrence = -1;
            return null;
        }

        public int findIndex(int tag)
        {
            int num = 0;
            this.tagToFind = tag;
            while (num < this.tags.Count)
            {
                TLVMsg msg = (TLVMsg) this.tags[num];
                if (msg.getTag() == tag)
                {
                    this.indexLastOccurrence = num;
                    return num;
                }
                num++;
            }
            this.indexLastOccurrence = -1;
            return -1;
        }

        public TLVMsg findNextTLV()
        {
            for (var i = this.indexLastOccurrence + 1; i < this.tags.Count; i++)
            {
                var msg = (TLVMsg) this.tags[i];
                if (msg.getTag() == this.tagToFind)
                {
                    this.indexLastOccurrence = i;
                    return msg;
                }
            }
            return null;
        }

        public string getString(int tag)
        {
            var msg = this.find(tag);
            return msg != null ? msg.getStringValue() : null;
        }

        private int getTAG(MemoryStream buffer)
        {
            int num = Convert.ToByte(buffer.ReadByte()) & 0xff;
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
            if ((num & 0x1f) == 0x1f)
            {
                do
                {
                    num2 = num2 << 8;
                    num = Convert.ToByte(buffer.ReadByte());
                    num2 |= num & 0xff;
                }
                while ((num & 0x80) == 0x80);
            }
            return num2;
        }

        private TLVMsg getTLVMsg(MemoryStream buffer)
        {
            int num = this.getTAG(buffer);
            if (num == 0)
            {
                return null;
            }
            byte[] buffer2 = null;
            if (buffer.Position >= buffer.Length)
            {
                throw new ISOException("BAD TLV FORMAT - tag (" + string.Format("{0:x}", num) + ") without length or value");
            }
            int num2 = this.getValueLength(buffer);
            if (num2 > (buffer.Length - buffer.Position))
            {
                throw new ISOException(string.Concat("BAD TLV FORMAT - tag (", string.Format("{0:x}", num), ") length (", num2, ") exceeds available data."));
            }
            if (num2 > 0)
            {
                // ReSharper disable once RedundantAssignment
                buffer2 = new byte[num2];
                buffer2 = buffer.GetBuffer();
            }
            return new TLVMsg(num, buffer2, "");
        }

        private int getValueLength(MemoryStream buffer)
        {
            var num2 = Convert.ToByte(buffer.ReadByte()) & 0xff;
            if ((num2 & 0x80) != 0x80) return num2;
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

        public TLVMsg index(int index)
        {
            return (TLVMsg) this.tags[index];
        }

        public byte[] pack()
        {
            var num = 0;
            var stream = new MemoryStream(400);
            while (num < this.tags.Count)
            {
                byte[] buffer = ((TLVMsg) this.tags[num]).getTLV();
                stream.Write(buffer, 0, buffer.Length);
                num++;
            }
            // ReSharper disable once UnusedVariable
            var buffer2 = new byte[stream.Position];
            stream.SetLength(stream.Position);
            stream.Position = 0L;
            return stream.GetBuffer();
        }

        public void unpack(byte[] buf)
        {
            var buffer = new MemoryStream(buf);
            while (Convert.ToByte(buffer.ReadByte()) != -1)
            {
                var tlvToAppend = this.getTLVMsg(buffer);
                if (tlvToAppend != null)
                {
                    this.append(tlvToAppend);
                }
            }
        }

        public void unpack(byte[] buf, int offset)
        {
            MemoryStream buffer = new MemoryStream(buf, offset, buf.Length - offset);
            while (buffer.Position < buffer.Length)
            {
                TLVMsg tlvToAppend = this.getTLVMsg(buffer);
                this.append(tlvToAppend);
            }
        }
    }
}

