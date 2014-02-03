using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Lab.Acq
{
    public static class DataServer
    {
        public const string PipePrefix = @"\\.\pipe\";

        public static bool IsRunning(string coordName)
        {
            return File.Exists(PipePrefix + coordName);
        }

        static Process LaunchExternal_NoWait(string implName, string coordinationName)
        {
            ProcessStartInfo ps = new ProcessStartInfo(implName + ".Impl.exe", coordinationName);
            ps.WindowStyle = ProcessWindowStyle.Minimized;
            return Process.Start(ps);
        }

        public static Process LaunchExternal(string implName, string coordinationName, bool waitUntilReady)
        {
            if (!waitUntilReady)
                return LaunchExternal_NoWait(implName, coordinationName);

            Process p;
            using (var s = BuildNamedSingletonSemaphore(coordinationName))
            {
                p = LaunchExternal_NoWait(implName, coordinationName);
                s.WaitOne();
            }
            return p;
        }

        public static Process LaunchExternal(string implName, bool waitUntilReady)
        {
            return LaunchExternal(implName, implName, waitUntilReady);
        }

        public static Semaphore BuildNamedSingletonSemaphore(string semaphoreName)
        {
            return new Semaphore(0, 1, semaphoreName);
        }

        internal static void ConsoleWait(CancellationTokenSource cancelSource)
        {
            Console.WriteLine("Started.  Press ESC to quit.");
            var wh = cancelSource.Token.WaitHandle;
            while (!wh.WaitOne(200))
            {
                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                    break;
            }
            Console.WriteLine("Exiting....");
            cancelSource.Cancel();
        }

    }
}
