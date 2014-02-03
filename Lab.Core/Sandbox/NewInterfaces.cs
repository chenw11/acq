using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab.Sandbox
{
    interface IStartStop : IDisposable
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
    }

    interface IDataHardware<TData> : IStartStop
    {
        IDataOutputMethod<TData> Outputter { get; set; }
    }

    interface IDataOutputMethod<TData>
    {
        /// <summary>
        /// Attempts to copy data into this object, using the given copier.  The copier should be 
        /// non-blocking and should not store a reference to the TData object that it writes to.
        /// </summary>
        /// <param name="copier">Copies data from input to TData output</param>
        /// <param name="inputData">Input data, or index or other object used to command copier delegate</param>
        /// <returns>True if copy was successful, false otherwise</returns>
        bool TryOutput<TIn>(Action<TIn, TData> copier, TIn inputData);
    }

    interface IHardwareInformation<THInfo>
    {
        THInfo HardwareInfo { get; }
    }

    interface IStandbyConfigurable<TSConfig> : IStartStop
    {
        TSConfig StandbyConfiguration { get; set; } // set fails when running
    }

    interface ILiveConfigurable<TLConfig> : IStartStop
    {
        TLConfig LiveConfiguration { get; set; }
    }

    interface ICamera :
        IStartStop,
        IDataHardware<Lab.Acq.VideoFrame>, 
        IStandbyConfigurable<Lab.Acq.VideoSettingsStatic>,
        IHardwareInformation<Lab.Acq.CameraAttributes>
    {
        
    }

}
