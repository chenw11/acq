using ProtoBuf.Meta;
using System;
using System.Threading;
using xq = QCamManagedDriver.QCam;

namespace Lab.Acq
{
    public class QCamServer : DataServer<IQCamFlow>
    {
        public QCamServer(TypeModel customSerializer)
            : base(new QCamCoordinator(),
            (r,c) => new QCamFlow(r,c),
            customSerializer) { }

        public QCamServer() : this(new AcqProtoSerializer()) { }

        static void Main(string[] args)
        {
            xq.QCamM_LoadDriver().Check();

            //Test2();
            new QCamServer().StandaloneServerMain(args);

            xq.QCamM_ReleaseDriver();
        }

        private static void Test()
        {
            var serverObj = new QCamServer();
            var cancelSource = new CancellationTokenSource();
            var serverTask = serverObj.LaunchServer(cancelSource.Token, waitUntilReady: true);

            var cr = new CameraReader<ICameraFlow, RemoteClientCameraFlow>(
                serverObj.DefaultCoordinationName,
                serverObj.customSerializer);
            cr.TerminateServerOnStop = true;
            var cam = cr.Flow;
            var src = cr.DataSource;

            var ss = cam.SettingsStatic;
            cam.SettingsStatic = ss;
            src.NewData += (s, e) => Console.WriteLine(e.FrameNumber); ;
            src.Start();

            while (true)
            {
                if (Console.KeyAvailable)
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        break;
                Thread.Sleep(100);
            }

            src.Stop();
            cam.Dispose();

            cancelSource.Cancel();
        }


        private static void Test2()
        {
            var hal = new QCamHAL();
            var offsetRange = hal.GetParameterRange(QCamManagedDriver.QCamM_ParamS32.qprmS32AbsoluteOffset);
            var gainRange = hal.GetParameterRange(QCamManagedDriver.QCamM_ParamS32.qprmS32NormalizedGaindB);

            Console.WriteLine("Offset range: " + offsetRange);
            Console.WriteLine("Gain range: " + gainRange);

            hal.Start();

            Console.WriteLine("Setting gain...");
            var d = hal.SettingsDynamic;
            d.AnalogGain_dB = 1.0f;
            hal.SettingsDynamic = d;
            Console.WriteLine("Done.");

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
        }
    }
}
