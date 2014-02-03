using System;
using System.Collections.Generic;
using System.Linq;


namespace Lab
{
    [Serializable]
    public class ValidationException : ApplicationException
    {
        public ValidationException(string msg) : base(msg) { }
        public ValidationException() { }
    }
}
