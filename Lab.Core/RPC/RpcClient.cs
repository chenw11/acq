using Lab.IO;
using Lab.RPC;
using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;

namespace Lab
{
    public class RpcClient : RpcBase, IRpcClient
    {
        readonly Type rpcIfaceType;

        internal void SendInitialHandshake() { LengthPrefixMessenger.ClientHandshake(stream); }

        public RpcClient(Stream stream, Type rpcIfaceType, TypeModel customSerializer)
            :base(stream, customSerializer)
        {
            if (rpcIfaceType == null)
                throw new ArgumentNullException("rpcIfaceType");
            if (!rpcIfaceType.IsInterface)
                throw new ArgumentException("expecting interface type");
            this.rpcIfaceType = rpcIfaceType;
        }




        public void RemoteCallVoid(string methodName, params object[] args)
        {
            validateMethodSignature(methodName, typeof(void), args);
            callRemote(methodName, args);
        }

        public T RemoteCall<T>(string methodName, params object[] args)
        {
            validateMethodSignature(methodName, typeof(T), args);
            var responseData = callRemote(methodName, args);
            return byteConverter.FromBytes<T>(responseData);
        }

        private readonly object commLock = new object();
        byte[] callRemote(string funcName, params object[] args)
        {
            RPCFuncCall c = new RPCFuncCall
            {
                FuncName = funcName,
                Args = Array.ConvertAll(args,
                    a => new RPCFuncArgVal { ArgValue = byteConverter.ToBytes(a) })
            };

            Trace.TraceInformation(string.Format("Sending calling function {0}... ", funcName));
            
            RPCFuncReturnVal r;
            lock (commLock)
            {
                messenger.SendMessage<RPCFuncCall>(c);
                var np = stream as NamedPipeClientStream;
                if (np != null)
                    np.WaitForPipeDrain();
                r = messenger.ReceiveMessage<RPCFuncReturnVal>();
            }
            if (r.Status == RPCStatus.ReturnOK)
                return r.RetValue;
            else if (r.Status == RPCStatus.FuncThrewException)
                throw new RpcRemoteException(r.ErrorMsg);
            else if (r.Status == RPCStatus.RPCError)
                throw new CommunicationException(r.ErrorMsg);
            else
                throw new CommunicationException("RPC server returned an invalid status response: " + r.Status);
        }

        [Conditional("DEBUG")]
        private void validateMethodSignature(string funcName, Type retType, object[] args)
        {
            var type = rpcIfaceType;
            var methods = Utilities.RecurseInterfaces<MethodInfo>(type, t => t.GetMethods()).Where(m => m.Name == funcName).Distinct().ToArray();
            if (methods.Length < 1)
                throw new ArgumentException("Invalid method name!");
            else if (methods.Length > 1)
                throw new ArgumentException("More than one method with this same in the inheritance hierarchy");

            MethodInfo mi = methods[0];
            var ps = mi.GetParameters();
            if (ps.Length != args.Length)
                throw new ArgumentException(string.Format(
                    "Invalid number of function args: Expecting {0} but got {1}",
                    ps.Length, args.Length));
            for (int i = 0; i < args.Length; i++)
                if (!ps[i].ParameterType.IsAssignableFrom(args[i].GetType()))
                    throw new ArgumentException("Type mismatch on argument " + i);
            if (mi.ReturnType != retType)
                throw new ArgumentException("Function " + funcName + " returns wrong type");
        }

    }

}
