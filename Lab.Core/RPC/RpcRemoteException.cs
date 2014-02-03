using System;

namespace Lab.RPC
{
    public class RpcRemoteException : Exception
    {
        public RpcRemoteException(string exceptionText)
            :base("Remote function threw an exception: " + exceptionText)
        {

        }
    }
}
