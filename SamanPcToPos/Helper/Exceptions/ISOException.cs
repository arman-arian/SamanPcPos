using System;
using System.IO;
using System.Text;

namespace SamanPcToPos.Helper.Exceptions
{
    public class ISOException : Exception
    {
        private readonly Exception nested;
        private const long serialVersionUID = -777216335204861186L;

        public ISOException()
        {
        }

        public ISOException(Exception nested) : base("", nested)
        {
            this.nested = nested;
        }

        public ISOException(string s) : base(s)
        {
        }

        public ISOException(string s, Exception nested) : base(s, nested)
        {
            this.nested = nested;
        }

        public void dump(StreamWriter p, string indent)
        {
            string str = indent + "  ";
            p.WriteLine(indent + "<" + this.getTagName() + ">");
            p.WriteLine(str + this.Message);
            if (this.nested != null)
            {
                var exception = this.nested as ISOException;
                if (exception != null)
                {
                    exception.dump(p, str);
                }
                else
                {
                    p.WriteLine(str + "<nested-exception>");
                    p.WriteLine(str);
                    p.WriteLine(this.StackTrace);
                    p.WriteLine(str + "</nested-exception>");
                }
            }
            p.Write(str);
            p.Write(this.StackTrace);
            p.WriteLine(indent + "</" + this.getTagName() + ">");
        }

        public Exception getNested()
        {
            return this.nested;
        }

        protected string getTagName()
        {
            return "iso-exception";
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(base.ToString());
            if (this.nested != null)
            {
                builder.Append(" (" + this.nested + ")");
            }
            return builder.ToString();
        }
    }
}

