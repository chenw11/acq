using System;

namespace Lab
{
    public interface IRpcClient : IDisposable
    {
        void RemoteCallVoid(string methodName, params object[] args);

        T RemoteCall<T>(string methodName, params object[] args);

        bool RequestRemoteTerminationOnDispose { get; set; }
    }
}
