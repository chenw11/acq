using System;

namespace Lab.IO
{
    public class CommunicationException : Exception
    {
        public CommunicationException(string errMsg)
            : base("Communication error: " + errMsg)
        {

        }

    }
}
