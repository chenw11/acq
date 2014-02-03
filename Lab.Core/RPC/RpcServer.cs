using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Lab.IO;
using Lab.RPC;
using ProtoBuf.Meta;

namespace Lab
{
    public class RpcServer<TIface> : RpcBase
    {
        readonly TIface implementation;
        public RpcServer(Stream connectedStream,
            TIface implementation, TypeModel customSerializer)
            :base(connectedStream, customSerializer)
        {
            if (implementation == null)
                throw new ArgumentNullException("implementation");
            this.implementation = implementation;
        }

        RPCFuncReturnVal DoMethodCall(RPCFuncCall call)
        {
            return RpcProcessor.DoMethodCall<TIface>(call,
                implementation, byteConverter, customSerializer);
        }


        readonly MemoryStream debug = new MemoryStream();


        /// <summary>
        /// Waits for an incoming message, processes it, and sends a response.
        /// </summary>
        public void ProcessMessage(out bool disconnect, out bool terminate)
        {
            terminate = false;
            disconnect = false;
            lock (messenger)
            {
                RPCFuncCall call;

                var status = messenger.TryReceiveMessage<RPCFuncCall>(out call);

                string msg;
                switch (status)
                {
                    case IO.NetServerControl.MessageFollows:
                        Trace.TraceInformation(NameForTrace + " got RPC request " + call.ToString());
                        RPCFuncReturnVal response = DoMethodCall(call);
                        messenger.SendMessage<RPCFuncReturnVal>(response);
                        Trace.TraceInformation(NameForTrace + " responded with " + response.ToString());
                        var np = stream as NamedPipeServerStream;
                        if (np != null)
                            np.WaitForPipeDrain();
                        return;
                    case IO.NetServerControl.Close:
                        msg = "Remote client requested to close connection";
                        disconnect = true;
                        break;
                    case IO.NetServerControl.TerminateServer:
                        terminate = true;
                        msg = "Remote client requested to terminate server";
                        break;
                    default:
                        msg = "Unexpected control byte response";
                        terminate = true;
                        base.stream.Dispose();
                        break;
                }
                Trace.TraceInformation(msg);
            }
        }

    }



}
