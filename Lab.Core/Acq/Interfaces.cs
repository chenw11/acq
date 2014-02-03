using System;
using System.Threading;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace Lab.Acq
{
    public interface IRingBufferWrite<T> where T : class
    {
        T RemoveBufferForWrite();
        void Write(T data);
    }

    public interface IOutputFileAndPipe : IDisposable
    {
        void SetOutputFile(string filePath);
        void SetOutputPipe(string pipeName);
    }

    public interface IStartStop : IDisposable
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
    }

    public static class StartStopExtensions
    {
        public static void ToggleRunningState(this IStartStop s)
        {
            if (s.IsRunning)
                s.Stop();
            else
                s.Start();
        }
    }

    public interface IStartStopDataSource<TData> : IStartStop
    {
        event EventHandler<TData> NewData;
    }

    public interface IFlow : IStartStop, IOutputFileAndPipe { }

    public interface ICamera : IStartStop
    {
        CameraAttributes CameraAttributes { get; }

        VideoSettingsStatic SettingsStatic { get; set; }

        VideoSettingsDynamic SettingsDynamic { get; set; }
    }

    public interface ICameraFlow : ICamera, IFlow { }

    public interface ISupportRingBufferOutput<T> where T : class
    {
        IRingBufferWrite<T> RingBufferForOutput { get; set; }
    }

    public interface ICameraHAL : ICamera, ISupportRingBufferOutput<VideoFrame> { }

    public interface ICoordinator
    {
        string DefaultCoordinationName { get; }
    }

    public interface ICoordinator<IFlow> : ICoordinator { }

    public interface IDataServer : ICoordinator
    {
        /// <summary>
        /// The size of the ring buffer used when sending data to the network pipe
        /// </summary>
        int RingBufferCapacity { get; set; }

        /// <summary>
        /// Launches the data server asynchronously, and optionally waits until it is ready to serve requests before returning
        /// </summary>
        Task LaunchServer(string coordinationName, CancellationToken cancelToken, bool waitUntilReady);
        
        void StandaloneServerMain(string[] args);
    }

    public static class ExtensionMethods
    {
        public static Task LaunchServer(this IDataServer server, CancellationToken cancelToken, bool waitUntilReady)
        {
            return server.LaunchServer(server.DefaultCoordinationName, cancelToken, waitUntilReady);
        }

        public static VideoSettingsStatic GetFullFrameSettings(this ICamera c)
        {
            var a = c.CameraAttributes;
            return new VideoSettingsStatic()
            {
                Binning = BinningMode.Binning1x1,
                Trigger = TriggeringMode.Freerun,
                Roi = new NaturalRect(0, 0, a.FullWidth, a.FullHeight)
            };
        }
    }

    public interface IDataServer<IFlow> : IDataServer, ICoordinator<IFlow> { }
}

