using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lab
{
    public class BasicResolver 
    {
        readonly Assembly[] assemblies;
        readonly Dictionary<Type, Type[]> implementations;

        /// <summary>
        /// Given an array of assemblies, returns an index allowing for fast retrieval of all 
        /// public classes with public parameterless constructors which implement an interface
        /// </summary>
        static Dictionary<Type, Type[]> BuildImplementationIndex(Assembly[] assemblies)
        {
            var all_impl = new Dictionary<Type, List<Type>>();
            foreach (Assembly a in assemblies)
                foreach (Type t in a.GetTypes())
                    if (t.IsPublic 
                        && t.IsClass
                        && (t.GetConstructor(Type.EmptyTypes) != null)
                        && (t.GetGenericArguments().Length == 0))
                    {
                        foreach (Type iface in t.GetInterfaces())
                        {
                            List<Type> t_implementors;
                            if (all_impl.TryGetValue(iface, out t_implementors))
                                t_implementors.Add(t);
                            else
                                all_impl.Add(iface, new List<Type>(new Type[] { t }));
                        }
                    }
            var ret = new Dictionary<Type, Type[]>();
            foreach (var kvp in all_impl)
                ret.Add(kvp.Key, kvp.Value.ToArray());
            return ret;
        }

        public BasicResolver(string[] assemblyNamesToUse)
        {
            this.assemblies = assemblyNamesToUse.Select(n => Assembly.LoadFrom(n)).ToArray();
            this.implementations = BuildImplementationIndex(assemblies);
        }

        public Type[] GetAvailableImplementations(Type iface)
        {
            if (iface == null)
                throw new ArgumentNullException();
            if (!iface.IsInterface)
                throw new ArgumentException("Expecting interface type");
            Type[] t_impl;
            if (implementations.TryGetValue(iface, out t_impl))
                return t_impl;
            else
                return Type.EmptyTypes;
        }

        public TIface Resolve<TIface>()
            where TIface : class
        {
            var impl = GetAvailableImplementations(typeof(TIface));
            if (impl.Length == 0)
                return null;
            else if (impl.Length > 1)
                throw new Exception("More than one implementation is available.");
            else
            {
                object o = impl[0].GetConstructor(Type.EmptyTypes).Invoke(null);
                return (TIface)(o);
            }
        }
    }
}
