using System;
using System.IO;
using System.Linq;
using ProtoBuf.Meta;

namespace Lab.RPC
{
    public static class RpcProcessor
    {

        /// <summary>
        /// Dynamically resolves and executes a method call serialized with 
        /// our Protobuf-based RPC scheme
        /// </summary>
        public static RPCFuncReturnVal DoMethodCall<TIface>(
            RPCFuncCall call, TIface impl,
            ProtoBufByteConverter byteConverter,
            TypeModel customSerializer)
        {
            if (call == null)
                return RPCFuncReturnVal.BuildRPCError("Server received empty RPC message");
            if (impl == null)
                throw new ArgumentNullException("impl");
            if (byteConverter == null)
                throw new ArgumentNullException("byteConverter");

            var methods = Utilities.RecurseInterfaces(typeof(TIface), t => t.GetMethods()).Where(m => m.Name == call.FuncName).Distinct().ToArray();
            if (methods.Length < 1)
                return RPCFuncReturnVal.BuildRPCError("RPC Function name not found " + call.FuncName);
            else if (methods.Length > 1)
                return RPCFuncReturnVal.BuildRPCError("More than one method found with name " + call.FuncName);

            var method = methods[0];
            if (method == null)
                return RPCFuncReturnVal.BuildRPCError("RPC Function name not found " + call.FuncName);
            var args = call.Args;
            if (args == null)
                args = new RPCFuncArgVal[0];
            var mParameters = method.GetParameters();
            if (mParameters.Length != args.Length)
                return RPCFuncReturnVal.BuildRPCError(
                    string.Format("RPC argument list for function {0} should have {1} elements, but was passed {1}",
                    call.FuncName, mParameters.Length, args.Length));

            MemoryStream ms = new MemoryStream();
            object[] parameters = new object[mParameters.Length];
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                var p = mParameters[i];

                if (a.ArgDef != null)
                {
                    if ((a.ArgDef.Name != p.Name) || (a.ArgDef.Type != p.ParameterType.Name))
                        return RPCFuncReturnVal.BuildRPCError(string.Format(
                            "RPC argument metadata was specified (is optional) and "
                            + "didn't match expected values: arg #{0} expected ({1} {2}) but given ({3} {4})",
                            i, p.ParameterType.Name, p.Name, a.ArgDef.Type, a.ArgDef.Name));
                }
                try
                {
                    parameters[i] = byteConverter.FromBytes(a.ArgValue, p.ParameterType);
                }
                catch (Exception e)
                {
                    return RPCFuncReturnVal.BuildRPCError(string.Format(
                        "Unable to deserialize argument #{0}: {1} {2} (given {3} bytes): {4}",
                        i, p.ParameterType.Name, p.Name, a.ArgValue.Length, e.ToString()));
                }
            }

            object retVal = null;
            try { retVal = method.Invoke(impl, parameters); }
            catch (Exception e)
            {
                return RPCFuncReturnVal.BuildFuncExceptionError(e.ToString());
            }
            byte[] retValBytes = (retVal == null) ? new byte[0] : byteConverter.ToBytes(retVal);
            return RPCFuncReturnVal.BuildOK(retValBytes);
        }
    }
}
