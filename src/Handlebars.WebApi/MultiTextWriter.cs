using System; 
using System.IO; 
using System.Text; 

namespace Handlebars.WebApi
{
    /// <summary>
    /// Spreads data out to multiple text writers.
    /// </summary>
    public class MultiTextWriter : TextWriter
    {

        #region Construction/Destruction
        public MultiTextWriter(params TextWriter[] writers)
        {
            _writers = writers;
        }
        #endregion 


        private readonly TextWriter[] _writers;
        private IFormatProvider formatProvider = null;
        private Encoding encoding = null;

        #region TextWriter Properties
        public override IFormatProvider FormatProvider
        {
            get
            {
                IFormatProvider formatProvider = this.formatProvider;
                if (formatProvider == null)
                {
                    formatProvider = base.FormatProvider;
                }
                return formatProvider;
            }
        }

        public override string NewLine
        {
            get { return base.NewLine; }

            set
            {
                for(var i = 0; i < _writers.Length; i++)                
                {
                    _writers[i].NewLine = value;
                }

                base.NewLine = value;
            }
        }


        public override Encoding Encoding
        {
            get
            {
                Encoding encoding = this.encoding;

                if (encoding == null)
                {
                    encoding = Encoding.Default;
                }

                return encoding;
            }
        }

        #region TextWriter Property Setters

        MultiTextWriter SetFormatProvider(IFormatProvider value)
        {
            this.formatProvider = value;
            return this;
        }

        MultiTextWriter SetEncoding(Encoding value)
        {
            this.encoding = value;
            return this;
        }
        #endregion // TextWriter Property Setters
        #endregion // TextWriter Properties
      

        #region TextWriter methods

        public override void Close()
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Close();
            }
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            for (var i = 0; i < _writers.Length; i++)
            {                
                if (disposing)
                {
                    _writers[i].Dispose();
                }
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Flush();
            }

            base.Flush();
        }

        //foreach (System.IO.TextWriter writer in this.writers)
        //{
        //    writer;
        //}
        public override void Write(bool value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(char value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(char[] buffer)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(buffer);
            }
        }

        public override void Write(decimal value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(double value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(float value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(int value)
        {
            for(var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(long value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(object value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(string value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(uint value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(ulong value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(value);
            }
        }

        public override void Write(string format, object arg0)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(format, arg0);
            }

        }

        public override void Write(string format, params object[] arg)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(format, arg);
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(buffer, index, count);
            }
        }

        public override void Write(string format, object arg0, object arg1)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(format, arg0, arg1);
            }
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].Write(format, arg0, arg1, arg2);
            }
        }

        public override void WriteLine()
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine();
            }
        }

        public override void WriteLine(bool value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(char value)
        {
            for(var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(char[] buffer)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(buffer);
            }
        }

        public override void WriteLine(decimal value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(double value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(float value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(int value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(long value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(object value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(string value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(uint value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(ulong value)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(value);
            }
        }

        public override void WriteLine(string format, object arg0)
        {
            for(var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(format, arg0);
            }
        }

        public override void WriteLine(string format, params object[] arg)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(format, arg);
            }
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(buffer, index, count);
            }
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(format, arg0, arg1);
            }
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            for (var i = 0; i < _writers.Length; i++)
            {
                _writers[i].WriteLine(format, arg0, arg1, arg2);
            }
        }
        #endregion 
    }
}
