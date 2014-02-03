using ProtoBuf.Meta;

namespace Lab.Acq
{
    public class FakeServer : DataServer<IFakeFlow>
    {
        public FakeServer(TypeModel customSerializer)
            : base(new FakeCoordinator(),
            (r,c) => new FakeFlow(r,c),
            customSerializer) { }

        public FakeServer() : this(new AcqProtoSerializer()) { }

        static void Main(string[] args)
        {
            new FakeServer().StandaloneServerMain(args);
        }
    }
}
