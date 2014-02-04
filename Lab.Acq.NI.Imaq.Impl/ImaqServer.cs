using ProtoBuf.Meta;
using System;
using System.Diagnostics;


namespace Lab.Acq
{
    public class ImaqServer : DataServer<IImaqFlow>
    {
        public ImaqServer(TypeModel customSerializer)
            : base(new ImaqCoordinator(),
            (r, c) => new ImaqFlow(r, c),
            customSerializer) { }

        public ImaqServer() : this(new AcqProtoSerializer()) { }

        static void Main(string[] args)
        {
            new ImaqServer().StandaloneServerMain(args);
        }

        static void test()
        {
            using (var flow = new ImaqFlow(8, new AcqProtoSerializer()))
            {
                flow.GotNewData += flow_GotNewData;
                flow.Start();
                while (!Console.KeyAvailable)
                {
                    System.Threading.Thread.Sleep(100);
                }
                flow.Stop();
            }
        }

        static void flow_GotNewData(VideoFrame obj)
        {
            Console.WriteLine("Got frame " + obj.FrameNumber);
        }
    }
}
