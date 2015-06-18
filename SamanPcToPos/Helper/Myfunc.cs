using System;
using System.Globalization;

namespace SamanPcToPos.Helper
{
    public static class Myfunc
    {
        public static bool InArray(int[] ar, int a)
        {
            for (var i = 0; i < (ar.Length - 1); i++)
            {
                if (ar[i] == a)
                {
                    return true;
                }
            }
            return false;
        }

        public static byte[] JoinArrays(byte[] Array1, byte[] Array2)
        {
            var array = new byte[Array1.Length + Array2.Length];
            Array1.CopyTo(array, 0);
            Array2.CopyTo(array, Array1.Length);
            return array;
        }

        public static byte[] JoinArrays(byte[] Array1, byte[] Array2, byte[] Array3)
        {
            var array = new byte[(Array1.Length + Array2.Length) + Array3.Length];
            Array1.CopyTo(array, 0);
            Array2.CopyTo(array, Array1.Length);
            Array3.CopyTo(array, Array1.Length + Array2.Length);
            return array;
        }

        public static byte[] JoinArrays(byte[] Array1, byte[] Array2, byte[] Array3, byte[] Array4)
        {
            var array = new byte[((Array1.Length + Array2.Length) + Array3.Length) + Array4.Length];
            Array1.CopyTo(array, 0);
            Array2.CopyTo(array, Array1.Length);
            Array3.CopyTo(array, Array1.Length + Array2.Length);
            Array4.CopyTo(array, (Array1.Length + Array2.Length) + Array3.Length);
            return array;
        }

        public static byte[] JoinArrays(byte[] Array1, byte[] Array2, byte[] Array3, byte[] Array4, byte[] Array5)
        {
            var array = new byte[(((Array1.Length + Array2.Length) + Array3.Length) + Array4.Length) + Array5.Length];
            Array1.CopyTo(array, 0);
            Array2.CopyTo(array, Array1.Length);
            Array3.CopyTo(array, Array1.Length + Array2.Length);
            Array4.CopyTo(array, (Array1.Length + Array2.Length) + Array3.Length);
            Array5.CopyTo(array, ((Array1.Length + Array2.Length) + Array3.Length) + Array4.Length);
            return array;
        }

        public static string SabtCheckDigit(string batchNo, string transNo)
        {
            byte[] buffer = { 0x12, 0x11, 0x10, 15, 14, 13, 12, 11, 10 };
            var str = batchNo.PadLeft(4, '0') + transNo.PadLeft(5, '0');
            var chArray = str.ToCharArray();
            var num = 0M;
            for (var i = 0; i < 9; i++)
            {
                num += buffer[i] * int.Parse(chArray[i].ToString());
            }
            var num3 = num % 99M;
            return (str + num3.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'));
        }

        public static byte[] ToByteArray(string HexString)
        {
            HexString = HexString.Replace(" ", "");
            var length = HexString.Length;
            var buffer = new byte[length / 2];
            for (var i = 0; i < length; i += 2)
            {
                buffer[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 0x10);
            }
            return buffer;
        }
    }
}

