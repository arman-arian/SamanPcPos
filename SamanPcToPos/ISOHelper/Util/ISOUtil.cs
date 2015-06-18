using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SamanPcToPos.Helper.Exceptions;

namespace SamanPcToPos.ISOHelper.Util
{
    public class ISOUtil
    {
        public static byte[] ASCII2EBCDIC = new byte[] { 
            0, 1, 2, 3, 0x37, 0x2d, 0x2e, 0x2f, 0x16, 5, 0x15, 11, 12, 13, 14, 15, 
            0x10, 0x11, 0x12, 0x13, 60, 0x3d, 50, 0x26, 0x18, 0x19, 0x3f, 0x27, 0x1c, 0x1d, 30, 0x1f, 
            0x40, 90, 0x7f, 0x7b, 0x5b, 0x6c, 80, 0x7d, 0x4d, 0x5d, 0x5c, 0x4e, 0x6b, 0x60, 0x4b, 0x61, 
            240, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0x7a, 0x5e, 0x4c, 0x7e, 110, 0x6f, 
            0x7c, 0xc1, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 200, 0xc9, 0xd1, 210, 0xd3, 0xd4, 0xd5, 0xd6, 
            0xd7, 0xd8, 0xd9, 0xe2, 0xe3, 0xe4, 0xe5, 230, 0xe7, 0xe8, 0xe9, 0xad, 0xe0, 0xbd, 0x5f, 0x6d, 
            0x79, 0x81, 130, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x91, 0x92, 0x93, 0x94, 0x95, 150, 
            0x97, 0x98, 0x99, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7, 0xa8, 0xa9, 0xc0, 0x4f, 0xd0, 0xa1, 7, 
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 6, 0x17, 40, 0x29, 0x2a, 0x2b, 0x2c, 9, 10, 0x1b, 
            0x30, 0x31, 0x1a, 0x33, 0x34, 0x35, 0x36, 8, 0x38, 0x39, 0x3a, 0x3b, 4, 20, 0x3e, 0xff, 
            0x41, 170, 0x4a, 0xb1, 0x9f, 0xb2, 0x6a, 0xb5, 0xbb, 180, 0x9a, 0x8a, 0xb0, 0xca, 0xaf, 0xbc, 
            0x90, 0x8f, 0xea, 250, 190, 160, 0xb6, 0xb3, 0x9d, 0xda, 0x9b, 0x8b, 0xb7, 0xb8, 0xb9, 0xab, 
            100, 0x65, 0x62, 0x66, 0x63, 0x67, 0x9e, 0x68, 0x74, 0x71, 0x72, 0x73, 120, 0x75, 0x76, 0x77, 
            0xac, 0x69, 0xed, 0xee, 0xeb, 0xef, 0xec, 0xbf, 0x80, 0xfd, 0xfe, 0xfb, 0xfc, 0xba, 0xae, 0x59, 
            0x44, 0x45, 0x42, 70, 0x43, 0x47, 0x9c, 0x48, 0x54, 0x51, 0x52, 0x53, 0x58, 0x55, 0x56, 0x57, 
            140, 0x49, 0xcd, 0xce, 0xcb, 0xcf, 0xcc, 0xe1, 0x70, 0xdd, 0xde, 0xdb, 220, 0x8d, 0x8e, 0xdf
         };
        public static byte[] EBCDIC2ASCII = new byte[] { 
            0, 1, 2, 3, 0x9c, 9, 0x86, 0x7f, 0x97, 0x8d, 0x8e, 11, 12, 13, 14, 15, 
            0x10, 0x11, 0x12, 0x13, 0x9d, 10, 8, 0x87, 0x18, 0x19, 0x92, 0x8f, 0x1c, 0x1d, 30, 0x1f, 
            0x80, 0x81, 130, 0x83, 0x84, 0x85, 0x17, 0x1b, 0x88, 0x89, 0x8a, 0x8b, 140, 5, 6, 7, 
            0x90, 0x91, 0x16, 0x93, 0x94, 0x95, 150, 4, 0x98, 0x99, 0x9a, 0x9b, 20, 0x15, 0x9e, 0x1a, 
            0x20, 160, 0xe2, 0xe4, 0xe0, 0xe1, 0xe3, 0xe5, 0xe7, 0xf1, 0xa2, 0x2e, 60, 40, 0x2b, 0x7c, 
            0x26, 0xe9, 0xea, 0xeb, 0xe8, 0xed, 0xee, 0xef, 0xec, 0xdf, 0x21, 0x24, 0x2a, 0x29, 0x3b, 0x5e, 
            0x2d, 0x2f, 0xc2, 0xc4, 0xc0, 0xc1, 0xc3, 0xc5, 0xc7, 0xd1, 0xa6, 0x2c, 0x25, 0x5f, 0x3e, 0x3f, 
            0xf8, 0xc9, 0xca, 0xcb, 200, 0xcd, 0xce, 0xcf, 0xcc, 0x60, 0x3a, 0x23, 0x40, 0x27, 0x3d, 0x22, 
            0xd8, 0x61, 0x62, 0x63, 100, 0x65, 0x66, 0x67, 0x68, 0x69, 0xab, 0xbb, 240, 0xfd, 0xfe, 0xb1, 
            0xb0, 0x6a, 0x6b, 0x6c, 0x6d, 110, 0x6f, 0x70, 0x71, 0x72, 170, 0xba, 230, 0xb8, 0xc6, 0xa4, 
            0xb5, 0x7e, 0x73, 0x74, 0x75, 0x76, 0x77, 120, 0x79, 0x7a, 0xa1, 0xbf, 0xd0, 0x5b, 0xde, 0xae, 
            0xac, 0xa3, 0xa5, 0xb7, 0xa9, 0xa7, 0xb6, 0xbc, 0xbd, 190, 0xdd, 0xa8, 0xaf, 0x5d, 180, 0xd7, 
            0x7b, 0x41, 0x42, 0x43, 0x44, 0x45, 70, 0x47, 0x48, 0x49, 0xad, 0xf4, 0xf6, 0xf2, 0xf3, 0xf5, 
            0x7d, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 80, 0x51, 0x52, 0xb9, 0xfb, 0xfc, 0xf9, 250, 0xff, 
            0x5c, 0xf7, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 90, 0xb2, 0xd4, 0xd6, 210, 0xd3, 0xd5, 
            0x30, 0x31, 50, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0xb3, 0xdb, 220, 0xd9, 0xda, 0x9f
         };
        public static byte ETX = 3;
        public static byte FS = 0x1c;
        public static byte GS = 30;
        public static byte RS = 0x1d;
        public static byte STX = 2;
        public static byte US = 0x1f;

        public static byte[] asciiToEbcdic(string s)
        {
            return asciiToEbcdic(Encoding.ASCII.GetBytes(s));
        }

        public static byte[] asciiToEbcdic(byte[] a)
        {
            byte[] buffer = new byte[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                buffer[i] = ASCII2EBCDIC[a[i] & 0xff];
            }
            return buffer;
        }

        public static void asciiToEbcdic(string s, byte[] e, int offset)
        {
            int length = s.Length;
            for (int i = 0; i < length; i++)
            {
                e[offset + i] = ASCII2EBCDIC[s[i] & '\x00ff'];
            }
        }

        public static string bcd2str(byte[] b, int offset, int len, bool padLeft)
        {
            StringBuilder builder = new StringBuilder(len);
            int num = (((len & 1) == 1) && padLeft) ? 1 : 0;
            for (int i = num; i < (len + num); i++)
            {
                int num3 = ((i & 1) == 1) ? 0 : 4;
                char c = (char) (((b[offset + (i >> 1)] >> num3) & 15) + 0x30);
                if (c == 'd')
                {
                    c = '=';
                }
                builder.Append(char.ToUpper(c));
            }
            return builder.ToString();
        }

        public static byte[] BitArray2byte(BitArray b)
        {
            int num = ((b.Length + 0x3e) >> 6) << 6;
            byte[] buffer = new byte[num >> 3];
            for (int i = 0; i < (num - 1); i++)
            {
                if (b[i + 1])
                {
                    buffer[i >> 3] = (byte) (buffer[i >> 3] | ((byte) (((int) 0x80) >> (i % 8))));
                }
            }
            if (num > 0x40)
            {
                buffer[0] = (byte) (buffer[0] | 0x80);
            }
            if (num > 0x80)
            {
                buffer[8] = (byte) (buffer[8] | 0x80);
            }
            return buffer;
        }

        public static byte[] BitArray2byte(BitArray b, int bytes)
        {
            int num = bytes * 8;
            byte[] buffer = new byte[bytes];
            for (int i = 0; i < (b.Length - 1); i++)
            {
                if (b[i + 1])
                {
                    buffer[i >> 3] = (byte) (buffer[i >> 3] | ((byte) (((int) 0x80) >> (i % 8))));
                }
            }
            if (num > 0x40)
            {
                buffer[0] = (byte) (buffer[0] | 0x80);
            }
            if (num > 0x80)
            {
                buffer[8] = (byte) (buffer[8] | 0x80);
            }
            return buffer;
        }

        public static byte[] BitArray2extendedByte(BitArray b)
        {
            int num = 0x80;
            byte[] buffer = new byte[num >> 3];
            for (int i = 0; i < num; i++)
            {
                if (b[i + 1])
                {
                    buffer[i >> 3] = (byte) (buffer[i >> 3] | ((byte) (((int) 0x80) >> (i % 8))));
                }
            }
            buffer[0] = (byte) (buffer[0] | 0x80);
            return buffer;
        }

        public static string BitArray2String(BitArray b)
        {
            int length = b.Length;
            length = (length > 0x80) ? 0x80 : length;
            StringBuilder builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                builder.Append(b[i] ? '1' : '0');
            }
            return builder.ToString();
        }

        public static string blankUnPad(string s)
        {
            return unPadRight(s, ' ');
        }

        public static BitArray byte2BitArray(byte[] b, int offset, bool bitZeroMeansExtended)
        {
            int length = bitZeroMeansExtended ? (((b[offset] & 0x80) == 0x80) ? 0x80 : 0x40) : 0x40;
            BitArray array = new BitArray(length, false);
            for (int i = 0; i < length; i++)
            {
                if ((b[offset + (i >> 3)] & (((int) 0x80) >> (i % 8))) > 0)
                {
                    array.Set(i + 1, true);
                }
            }
            return array;
        }

        public static BitArray byte2BitArray(byte[] b, int offset, int maxBits)
        {
            int num = (maxBits > 0x40) ? (((b[offset] & 0x80) == 0x80) ? 0x80 : 0x40) : maxBits;
            if (((maxBits > 0x80) && (b.Length > (offset + 8))) && ((b[offset + 8] & 0x80) == 0x80))
            {
                num = 0xc0;
            }
            BitArray array = new BitArray(num + 1, false);
            for (int i = 0; i < num; i++)
            {
                if ((b[offset + (i >> 3)] & (((int) 0x80) >> (i % 8))) > 0)
                {
                    array.Set(i + 1, true);
                }
            }
            return array;
        }

        public static BitArray byte2BitArray(BitArray bmap, byte[] b, int bitOffset)
        {
            int num = b.Length << 3;
            for (int i = 0; i < num; i++)
            {
                if ((b[i >> 3] & (((int) 0x80) >> (i % 8))) > 0)
                {
                    bmap.Set((bitOffset + i) + 1, true);
                }
            }
            return bmap;
        }

        public static byte[] CalculateMAC(byte[] mpk, byte[] message)
        {
            DESCryptoServiceProvider provider = new DESCryptoServiceProvider {
                Padding = PaddingMode.Zeros
            };
            provider.Key = mpk;
            provider.IV = new byte[8];
            provider.Mode = CipherMode.CBC;
            byte[] sourceArray = provider.CreateEncryptor(mpk, new byte[8]).TransformFinalBlock(message, 0, message.Length);
            byte[] destinationArray = new byte[8];
            Array.Copy(sourceArray, ((sourceArray.Length / 8) - 1) * 8, destinationArray, 0, 8);
            return destinationArray;
        }

        public static byte[] concat(byte[] array1, byte[] array2)
        {
            byte[] destinationArray = new byte[array1.Length + array2.Length];
            Array.Copy(array1, destinationArray, array1.Length);
            Array.Copy(array2, 0, destinationArray, array1.Length, array2.Length);
            return destinationArray;
        }

        public static byte[] concat(byte[] array1, int beginIndex1, int Length1, byte[] array2, int beginIndex2, int Length2)
        {
            byte[] destinationArray = new byte[Length1 + Length2];
            Array.Copy(array1, 0, destinationArray, 0, Length1);
            Array.Copy(array2, 0, destinationArray, Length1, array2.Length);
            return destinationArray;
        }

        public static byte[] DesDecrypt(byte[] cryptedString, byte[] key)
        {
            DESCryptoServiceProvider provider = new DESCryptoServiceProvider {
                Padding = PaddingMode.Zeros
            };
            return provider.CreateDecryptor(key, new byte[8]).TransformFinalBlock(cryptedString, 0, cryptedString.Length);
        }

        public static byte[] DesEncrypt(byte[] cryptedString, byte[] key)
        {
            DESCryptoServiceProvider provider = new DESCryptoServiceProvider {
                Padding = PaddingMode.Zeros
            };
            return provider.CreateEncryptor(key, new byte[8]).TransformFinalBlock(cryptedString, 0, cryptedString.Length);
        }

        public static string dumpString(byte[] b)
        {
            StringBuilder builder = new StringBuilder(b.Length * 2);
            for (int i = 0; i < b.Length; i++)
            {
                char c = (char) b[i];
                if (char.IsControl(c))
                {
                    switch (c)
                    {
                        case '\0':
                        {
                            builder.Append("{NULL}");
                            continue;
                        }
                        case '\x0001':
                        {
                            builder.Append("{SOH}");
                            continue;
                        }
                        case '\x0002':
                        {
                            builder.Append("{STX}");
                            continue;
                        }
                        case '\x0003':
                        {
                            builder.Append("{ETX}");
                            continue;
                        }
                        case '\x0004':
                        {
                            builder.Append("{EOT}");
                            continue;
                        }
                        case '\x0005':
                        {
                            builder.Append("{ENQ}");
                            continue;
                        }
                        case '\x0006':
                        {
                            builder.Append("{ACK}");
                            continue;
                        }
                        case '\a':
                        {
                            builder.Append("{BEL}");
                            continue;
                        }
                        case '\n':
                        {
                            builder.Append("{LF}");
                            continue;
                        }
                        case '\r':
                        {
                            builder.Append("{CR}");
                            continue;
                        }
                        case ' ':
                        {
                            builder.Append("{DLE}");
                            continue;
                        }
                        case '%':
                        {
                            builder.Append("{NAK}");
                            continue;
                        }
                        case '&':
                        {
                            builder.Append("{SYN}");
                            continue;
                        }
                        case '4':
                        {
                            builder.Append("{FS}");
                            continue;
                        }
                        case '6':
                        {
                            builder.Append("{RS}");
                            continue;
                        }
                    }
                    char ch2 = (char) ((b[i] >> 4) & 0x3f);
                    char ch3 = (char) (b[i] & 0x3f);
                    builder.Append('[');
                    builder.Append(char.ToUpper(ch2));
                    builder.Append(char.ToUpper(ch3));
                    builder.Append(']');
                    continue;
                }
                builder.Append(c);
            }
            return builder.ToString();
        }

        public static string ebcdicToAscii(byte[] e)
        {
            try
            {
                return Encoding.ASCII.GetString(ebcdicToAsciiBytes(e, 0, e.Length));
            }
            catch (Exception exception)
            {
                return exception.ToString();
            }
        }

        public static string ebcdicToAscii(byte[] e, int offset, int len)
        {
            try
            {
                return Encoding.ASCII.GetString(ebcdicToAsciiBytes(e, offset, len));
            }
            catch (Exception exception)
            {
                return exception.ToString();
            }
        }

        public static byte[] ebcdicToAsciiBytes(byte[] e)
        {
            return ebcdicToAsciiBytes(e, 0, e.Length);
        }

        public static byte[] ebcdicToAsciiBytes(byte[] e, int offset, int len)
        {
            byte[] buffer = new byte[len];
            for (int i = 0; i < len; i++)
            {
                buffer[i] = EBCDIC2ASCII[e[offset + i] & 0xff];
            }
            return buffer;
        }

        public static string formatAmount(long l, int len)
        {
            string s = l.ToString();
            if (l < 100L)
            {
                s = zeropad(s, 3);
            }
            StringBuilder builder = new StringBuilder(padleft(s, len - 1, ' '));
            builder.Insert(len - 3, '.');
            return builder.ToString();
        }

        public static string formatDouble(double d, int len)
        {
            string s = ((long) d).ToString();
            string str2 = ((int) (Math.Round((double) (d * 100.0)) % 100.0)).ToString();
            try
            {
                if (len > 3)
                {
                    s = padleft(s, len - 3, ' ');
                }
                str2 = zeropad(str2, 2);
            }
            catch (ISOException)
            {
            }
            return (s + "." + str2);
        }

        public static byte GetDigit(byte b)
        {
            if ((b >= 0x30) && (b <= 0x39))
            {
                return (byte) (b - 0x30);
            }
            if ((b < 0x41) || (b > 70))
            {
                throw new ISOException("Invalid Hex String");
            }
            return (byte) ((10 + b) - 0x41);
        }

        public static BitArray hex2BitArray(byte[] b, int offset, bool bitZeroMeansExtended)
        {
            int length = bitZeroMeansExtended ? ((((b[offset] - 0x30) & 8) == 8) ? 0x80 : 0x40) : 0x40;
            BitArray array = new BitArray(length, false);
            for (int i = 0; i < length; i++)
            {
                int num3 = b[offset + (i >> 2)] - 0x30;
                if ((num3 & (((int) 8) >> (i % 4))) > 0)
                {
                    array.Set(i + 1, true);
                }
            }
            return array;
        }

        public static BitArray hex2BitArray(byte[] b, int offset, int maxBits)
        {
            int num = (maxBits > 0x40) ? (((GetDigit(b[offset]) & 8) == 8) ? 0x80 : 0x40) : maxBits;
            BitArray array = new BitArray(num + 1, false);
            for (int i = 0; i < num; i++)
            {
                if ((GetDigit(b[offset + (i >> 2)]) & (((int) 8) >> (i % 4))) > 0)
                {
                    array.Set(i + 1, true);
                    if ((i == 0x41) && (maxBits > 0x80))
                    {
                        num = 0xc0;
                    }
                }
            }
            return array;
        }

        public static BitArray hex2BitArray(BitArray bmap, byte[] b, int bitOffset)
        {
            int num = b.Length << 2;
            for (int i = 0; i < num; i++)
            {
                if ((GetDigit(b[i >> 2]) & (((int) 8) >> (i % 4))) > 0)
                {
                    bmap.Set((bitOffset + i) + 1, true);
                }
            }
            return bmap;
        }

        public static byte[] hex2byte(string s)
        {
            if ((s.Length % 2) == 0)
            {
                return hex2byte(Encoding.ASCII.GetBytes(s), 0, s.Length >> 1);
            }
            return hex2byte("0" + s);
        }

        public static byte[] hex2byte(byte[] b, int offset, int len)
        {
            byte[] buffer = new byte[len];
            for (int i = 0; i < (len * 2); i++)
            {
                int num2 = ((i % 2) == 1) ? 0 : 4;
                buffer[i >> 1] = (byte) (buffer[i >> 1] | ((byte) (GetDigit(b[i]) << num2)));
            }
            return buffer;
        }

        public static string hexdump(byte[] b)
        {
            return hexdump(b, 0, b.Length);
        }

        public static string hexdump(byte[] b, int offset, int len)
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            StringBuilder builder3 = new StringBuilder();
            string str = "  ";
            string str2 = "\r\n";
            for (int i = offset; i < len; i++)
            {
                char c = (char) ((b[i] >> 4) & 0x3f);
                char ch2 = (char) (b[i] & 0x3f);
                builder2.Append(char.ToUpper(c));
                builder2.Append(char.ToUpper(ch2));
                builder2.Append(' ');
                char ch3 = (char) b[i];
                builder3.Append(((ch3 >= ' ') && (ch3 < '\x007f')) ? ch3 : '.');
                switch ((i % 0x10))
                {
                    case 7:
                        builder2.Append(' ');
                        break;

                    case 15:
                        builder.Append(hexOffset(i));
                        builder.Append(str);
                        builder.Append(builder2.ToString());
                        builder.Append(' ');
                        builder.Append(builder3.ToString());
                        builder.Append(str2);
                        builder2 = new StringBuilder();
                        builder3 = new StringBuilder();
                        break;
                }
            }
            if (builder2.Length > 0)
            {
                while (builder2.Length < 0x31)
                {
                    builder2.Append(' ');
                }
                builder.Append(hexOffset(len));
                builder.Append(str);
                builder.Append(builder2.ToString());
                builder.Append(' ');
                builder.Append(builder3.ToString());
                builder.Append(str2);
            }
            return builder.ToString();
        }

        private static string hexOffset(int i)
        {
            i = (i >> 4) << 4;
            int len = (i > 0xffff) ? 8 : 4;
            try
            {
                return zeropad(i.ToString(), len);
            }
            catch (ISOException exception)
            {
                return exception.Message;
            }
        }

        public static string hexor(string op1, string op2)
        {
            return hexString(xor(hex2byte(op1), hex2byte(op2)));
        }

        public static string hexString(byte[] b)
        {
            StringBuilder builder = new StringBuilder(b.Length * 2);
            for (int i = 0; i < b.Length; i++)
            {
                char c = (char) (((b[i] >> 4) & 15) + 0x30);
                char ch2 = (char) ((b[i] & 15) + 0x30);
                c = (c <= '9') ? c : ((char) (((c - '9') + 0x41) - 1));
                ch2 = (ch2 <= '9') ? ch2 : ((char) (((ch2 - '9') + 0x41) - 1));
                builder.Append(char.ToUpper(c));
                builder.Append(char.ToUpper(ch2));
            }
            return builder.ToString();
        }

        public static string hexString(byte[] b, int offset, int len)
        {
            StringBuilder builder = new StringBuilder(len * 2);
            len += offset;
            for (int i = offset; i < len; i++)
            {
                char c = (char) ((b[i] >> 4) & 0x3f);
                char ch2 = (char) (b[i] & 0x3f);
                builder.Append(char.ToUpper(c));
                builder.Append(char.ToUpper(ch2));
            }
            return builder.ToString();
        }

        public static bool isAlphaNumeric(string s)
        {
            int num = 0;
            int length = s.Length;
            while (((num < length) && (((char.IsLetterOrDigit(s[num]) || (s[num] == ' ')) || ((s[num] == '.') || (s[num] == '-'))) || (s[num] == '_'))) || (s[num] == '?'))
            {
                num++;
            }
            return (num >= length);
        }

        public static bool isBlank(string s)
        {
            return (s.Trim().Length == 0);
        }

        public static bool isNumeric(string s, int radix)
        {
            try
            {
                int.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool isZero(string s)
        {
            int num = 0;
            int length = s.Length;
            while ((num < length) && (s[num] == '0'))
            {
                num++;
            }
            return (num >= length);
        }

        public static string normalize(string s)
        {
            return normalize(s, true);
        }

        public static string normalize(string s, bool canonical)
        {
            StringBuilder builder = new StringBuilder();
            int num = (s != null) ? s.Length : 0;
            for (int i = 0; i < num; i++)
            {
                char ch = s[i];
                switch (ch)
                {
                    case '<':
                    {
                        builder.Append("&lt;");
                        continue;
                    }
                    case '>':
                    {
                        builder.Append("&gt;");
                        continue;
                    }
                    case '&':
                    {
                        builder.Append("&amp;");
                        continue;
                    }
                    case '"':
                    {
                        builder.Append("&quot;");
                        continue;
                    }
                    case '\n':
                    case '\r':
                    {
                        if (canonical)
                        {
                            builder.Append("&#");
                            builder.Append(((int) (ch & '\x00ff')).ToString());
                            builder.Append(';');
                        }
                        else if (ch < ' ')
                        {
                            builder.Append("&#");
                            builder.Append(((int) (ch & '\x00ff')).ToString());
                            builder.Append(';');
                        }
                        else if (ch > 0xff00)
                        {
                            builder.Append((char) (ch & '\x00ff'));
                        }
                        else
                        {
                            builder.Append(ch);
                        }
                        continue;
                    }
                }
                if (ch < ' ')
                {
                    builder.Append("&#");
                    builder.Append(((int) (ch & '\x00ff')).ToString());
                    builder.Append(';');
                }
                else if (ch > 0xff00)
                {
                    builder.Append((char) (ch & '\x00ff'));
                }
                else
                {
                    builder.Append(ch);
                }
            }
            return builder.ToString();
        }

        public static string padleft(string s, int len, char c)
        {
            s = s.Trim();
            if (s.Length > len)
            {
                throw new ISOException(string.Concat(new object[] { "invalid len ", s.Length, "/", len }));
            }
            StringBuilder builder = new StringBuilder(len);
            int num = len - s.Length;
            while (num-- > 0)
            {
                builder.Append(c);
            }
            builder.Append(s);
            return builder.ToString();
        }

        public static string padright(string s, int len, char c)
        {
            s = s.Trim();
            if (s.Length > len)
            {
                throw new ISOException(string.Concat(new object[] { "invalid len ", s.Length, "/", len }));
            }
            StringBuilder builder = new StringBuilder(len);
            int num = len - s.Length;
            builder.Append(s);
            while (num-- > 0)
            {
                builder.Append(c);
            }
            return builder.ToString();
        }

        public static int parseInt(string s)
        {
            return parseInt(s, 10);
        }

        public static int parseInt(byte[] bArray)
        {
            return parseInt(bArray, 10);
        }

        public static int parseInt(char[] cArray)
        {
            return parseInt(cArray, 10);
        }

        public static int parseInt(string s, int radix)
        {
            int length = s.Length;
            if (length > 9)
            {
                throw new Exception("Number can have maximum 9 digits");
            }
            int num2 = 0;
            int num3 = 0;
            int num4 = s[num3++] - '0';
            if (num4 == -1)
            {
                throw new Exception("String contains non-digit");
            }
            num2 = num4;
            while (num3 < length)
            {
                num2 *= radix;
                num4 = s[num3++] - '0';
                if (num4 == -1)
                {
                    throw new Exception("String contains non-digit");
                }
                num2 += num4;
            }
            return num2;
        }

        public static int parseInt(byte[] bArray, int radix)
        {
            int length = bArray.Length;
            if (length > 9)
            {
                throw new Exception("Number can have maximum 9 digits");
            }
            int num2 = 0;
            int num3 = 0;
            int num4 = (ushort) (bArray[num3++] - 0x30);
            if (num4 == -1)
            {
                throw new Exception("Byte array contains non-digit");
            }
            num2 = num4;
            while (num3 < length)
            {
                num2 *= radix;
                num4 = (ushort) (bArray[num3++] - 0x30);
                if (num4 == -1)
                {
                    throw new Exception("Byte array contains non-digit");
                }
                num2 += num4;
            }
            return num2;
        }

        public static int parseInt(char[] cArray, int radix)
        {
            int length = cArray.Length;
            if (length > 9)
            {
                throw new Exception("Number can have maximum 9 digits");
            }
            int num2 = 0;
            int num3 = 0;
            int num4 = cArray[num3++] - '0';
            if (num4 == -1)
            {
                throw new Exception("Char array contains non-digit");
            }
            num2 = num4;
            while (num3 < length)
            {
                num2 *= radix;
                num4 = cArray[num3++] - '0';
                if (num4 == -1)
                {
                    throw new Exception("Char array contains non-digit");
                }
                num2 += num4;
            }
            return num2;
        }

        public static string protect(string s)
        {
            StringBuilder builder = new StringBuilder();
            int length = s.Length;
            int num2 = (length > 6) ? 6 : 0;
            int num3 = -1;
            if (num2 > 0)
            {
                num3 = s.IndexOf('=') - 4;
                if (num3 < 0)
                {
                    num3 = s.IndexOf('^') - 4;
                    if (num3 < 0)
                    {
                        num3 = length - 4;
                    }
                }
            }
            for (int i = 0; i < length; i++)
            {
                if (s[i] == '=')
                {
                    num2 = 1;
                }
                else if (s[i] == '^')
                {
                    num3 = 0;
                    num2 = length - i;
                }
                else if (i == num3)
                {
                    num2 = 4;
                }
                builder.Append((num2-- > 0) ? s[i] : '_');
            }
            return builder.ToString();
        }

        public static void sleep(long millis)
        {
            try
            {
                Thread.Sleep((int) millis);
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        public static byte[] str2bcd(string s, bool padLeft)
        {
            byte[] d = new byte[(s.Length + 1) >> 1];
            return str2bcd(s, padLeft, d, 0);
        }

        public static byte[] str2bcd(string s, bool padLeft, byte fill)
        {
            int length = s.Length;
            byte[] buffer = new byte[(length + 1) >> 1];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = fill;
            }
            int num3 = (((length & 1) == 1) && padLeft) ? 1 : 0;
            for (int j = num3; j < (length + num3); j++)
            {
                buffer[j >> 1] = (byte) (buffer[j >> 1] | ((byte) ((((byte) s[j - num3]) - 0x30) << (((j & 1) == 1) ? 0 : 4))));
            }
            return buffer;
        }

        public static byte[] str2bcd(string s, bool padLeft, byte[] d, int offset)
        {
            int length = s.Length;
            int num2 = (((length & 1) == 1) && padLeft) ? 1 : 0;
            for (int i = num2; i < (length + num2); i++)
            {
                d[offset + (i >> 1)] = (byte) (d[offset + (i >> 1)] | ((byte) (GetDigit((byte) s[i - num2]) << (((i & 1) == 1) ? 0 : 4))));
            }
            return d;
        }

        public static byte[] str2bcd(string s, bool padLeft, byte[] d, int offset, int padVal)
        {
            int length = s.Length;
            int num2 = (((length & 1) == 1) && padLeft) ? 1 : 0;
            for (int i = num2; i < (length + num2); i++)
            {
                d[offset + (i >> 1)] = (byte) (d[offset + (i >> 1)] | ((byte) (GetDigit((byte) s[i - num2]) << (((i & 1) == 1) ? 0 : 4))));
            }
            if ((s.Length % 2) != 0)
            {
                d[((offset + length) + num2) - 1] = (byte) (d[((offset + length) + num2) - 1] | ((byte) padVal));
            }
            return d;
        }

        public static string strpad(string s, int len)
        {
            StringBuilder builder = new StringBuilder(s);
            while (builder.Length < len)
            {
                builder.Append(' ');
            }
            return builder.ToString();
        }

        public static string strpadf(string s, int len)
        {
            StringBuilder builder = new StringBuilder(s);
            while (builder.Length < len)
            {
                builder.Append('F');
            }
            return builder.ToString();
        }

        public static string takeFirstN(string s, int n)
        {
            if (s.Length > n)
            {
                return s.Substring(0, n);
            }
            if (s.Length < n)
            {
                return zeropad(s, n);
            }
            return s;
        }

        public static string takeLastN(string s, int n)
        {
            if (s.Length > n)
            {
                return s.Substring(s.Length - n);
            }
            if (s.Length < n)
            {
                return zeropad(s, n);
            }
            return s;
        }

        public static int[] toIntArray(string s)
        {
            string[] strArray = s.Split(new char[] { ' ' });
            int[] numArray = new int[strArray.Length];
            for (int i = 0; i < strArray.Length; i++)
            {
                numArray[i] = int.Parse(strArray[i]);
            }
            return numArray;
        }

        public static string[] ToStringArray(string s)
        {
            return s.Split(new char[] { ' ' });
        }

        public static string trim(string s)
        {
            if (s == null)
            {
                return null;
            }
            return s.Trim();
        }

        public static byte[] trim(byte[] array, int Length)
        {
            byte[] destinationArray = new byte[Length];
            Array.Copy(array, destinationArray, Length);
            return destinationArray;
        }

        public static string trimf(string s)
        {
            if (s != null)
            {
                int length = s.Length;
                if (length <= 0)
                {
                    return s;
                }
                while (--length >= 0)
                {
                    if (s[length] != 'F')
                    {
                        break;
                    }
                }
                s = (length == 0) ? "" : s.Substring(0, length + 1);
            }
            return s;
        }

        public static string unPadLeft(string s, char c)
        {
            int startIndex = 0;
            int length = s.Length;
            if (length != 0)
            {
                while ((startIndex < length) && (s[startIndex] == c))
                {
                    startIndex++;
                }
                if (startIndex >= length)
                {
                    return s.Substring(startIndex - 1, length);
                }
                return s.Substring(startIndex, length);
            }
            return s;
        }

        public static string unPadRight(string s, char c)
        {
            int length = s.Length;
            if (length != 0)
            {
                while ((0 < length) && (s[length - 1] == c))
                {
                    length--;
                }
                if (0 >= length)
                {
                    return s.Substring(0, 1);
                }
                return s.Substring(0, length);
            }
            return s;
        }

        public static byte[] xor(byte[] op1, byte[] op2)
        {
            byte[] buffer = null;
            if (op2.Length > op1.Length)
            {
                buffer = new byte[op1.Length];
            }
            else
            {
                buffer = new byte[op2.Length];
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte) (op1[i] ^ op2[i]);
            }
            return buffer;
        }

        public static string zeropad(string s, int len)
        {
            return padleft(s, len, '0');
        }

        public static string zeropadRight(string s, int len)
        {
            StringBuilder builder = new StringBuilder(s);
            while (builder.Length < len)
            {
                builder.Append('0');
            }
            return builder.ToString();
        }

        public static string zeroUnPad(string s)
        {
            return unPadLeft(s, '0');
        }
    }
}

