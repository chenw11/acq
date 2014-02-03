using System;

namespace Lab
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public class RpcWrapperAttribute : Attribute 
    {
        readonly Type wrappedInterface;

        public RpcWrapperAttribute(Type wrappedInterface)
        {
            if (wrappedInterface == null)
                throw new ArgumentNullException();
            if (!wrappedInterface.IsInterface)
                throw new ArgumentException("Expecting interface");

            this.wrappedInterface = wrappedInterface;
        }
    }
}
