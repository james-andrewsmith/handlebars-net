using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handlebars.Proxy
{
    public sealed class Trace : IDisposable
    {
        private Stopwatch _sw;
        private string _name; 
   
        public Trace(string name)
        {
            _name = name;
            _sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _sw.Stop();
            Console.WriteLine(_name + ":" + _sw.ElapsedMilliseconds + "ms");
        }
    }
}
