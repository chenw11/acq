using ProtoBuf;
using System;

namespace Lab.RPC
{

    [ProtoContract]
    public class RPCFuncArgDecl
    {
        [ProtoMember(1, IsRequired = true)]
        public string Name { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string Type { get; set; }
    }

    [ProtoContract]
    public class RPCFuncDecl
    {
        [ProtoMember(1, IsRequired = true)]
        public string FuncName { get; set; }

        [ProtoMember(2, IsRequired = false)]
        public string ReturnType { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public RPCFuncArgDecl[] Args { get; set; }
    }

    [ProtoContract]
    public class RPCFuncArgVal
    {
        [ProtoMember(1, IsRequired = false)]
        public RPCFuncArgDecl ArgDef { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public byte[] ArgValue { get; set; }
    }

    [ProtoContract]
    public class RPCFuncCall
    {
        [ProtoMember(1, IsRequired = true)]
        public string FuncName { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public RPCFuncArgVal[] Args { get; set; }

        public override string ToString()
        {
            if (Args == null)
                return FuncName;
            else
                return string.Format(FuncName + "(" + Args.Length + ")");
        }
    }

    public enum RPCStatus
    {
        UnknownError = 0,
        ReturnOK = 1,
        RPCError = 2,
        FuncThrewException = 3,
    }

    [ProtoContract]
    public class RPCFuncReturnVal
    {
        [ProtoMember(1, IsRequired=true)]
        public RPCStatus Status { get; set; }

        [ProtoMember(2, IsRequired=false)]
        public byte[] RetValue { get; set; }

        [ProtoMember(3, IsRequired=false)]
        public string ErrorMsg { get; set; }

        [ProtoMember(10, IsRequired = false)]
        public string ReturnType { get; set; }

        public RPCFuncReturnVal() { }

        public static RPCFuncReturnVal BuildRPCError(string errMsg)
        {
            return new RPCFuncReturnVal
            {
                Status = RPCStatus.RPCError,
                ErrorMsg = errMsg
            };
        }

        public static RPCFuncReturnVal BuildFuncExceptionError(string errMsg)
        {
            return new RPCFuncReturnVal
            {
                Status = RPCStatus.FuncThrewException,
                ErrorMsg = errMsg
            };
        }

        public static RPCFuncReturnVal BuildOK(byte[] data)
        {
            return new RPCFuncReturnVal
            {
                Status = RPCStatus.ReturnOK,
                RetValue = data,
            };
        }

        public override string ToString()
        {
            return Status.ToString();
        }
    }
}
