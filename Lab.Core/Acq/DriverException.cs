using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab.Acq
{
    public class DriverException : ApplicationException
    {
        public DriverException(string message) : base(message) { }

        public DriverException(int errorCode, string message)
            : base("Error " + errorCode + " " + message) { }
    }
}
