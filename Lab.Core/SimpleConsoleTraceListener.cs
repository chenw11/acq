using System;
using System.Diagnostics;
using ProtoBuf;

namespace Lab
{
    public class SimpleConsoleTraceListener : ConsoleTraceListener
    {
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            Console.WriteLine("{0}: {1}", eventType, message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            this.TraceEvent(eventCache, source, eventType, id, string.Format(format, args));
        }
    }
}
