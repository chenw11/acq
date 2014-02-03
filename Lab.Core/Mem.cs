using System;
using System.Reflection.Emit;

namespace Lab
{
    /// <summary>
    /// Provides pure CLR methods for handling raw memory.
    /// The idea is this should be fast, portable, and reasonably safe
    /// </summary>
    internal static class Mem
    {
        static Mem()
        {
            if ((_Copy.copy == null) || (_Set.set == null))
                throw new Exception("IL generation failed.");
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes.cpblk
        /// </summary>
        static class _Copy
        {
            public delegate void CopyFunc(IntPtr des, IntPtr src, uint bytes);
            public static readonly CopyFunc copy;

            static _Copy()
            {
                var dynamicMethod = new DynamicMethod
                (
                    "Copy",
                    typeof(void),
                    new[] { typeof(IntPtr), typeof(IntPtr), typeof(uint) },
                    typeof(_Copy)
                );

                var ilGenerator = dynamicMethod.GetILGenerator();

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldarg_2);

                ilGenerator.Emit(OpCodes.Cpblk);
                ilGenerator.Emit(OpCodes.Ret);

                copy = (CopyFunc)dynamicMethod.CreateDelegate(typeof(CopyFunc));
            }
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes.initblk
        /// </summary>
        static class _Set
        {
            public delegate void SetFunc(IntPtr des, byte value, uint numBytes);

            public static readonly SetFunc set;

            static _Set()
            {
                var dynamicMethod = new DynamicMethod
                (
                    "Set",
                    typeof(void),
                    new[] { typeof(IntPtr), typeof(byte), typeof(uint) },
                    typeof(_Set)
                );

                var ilGenerator = dynamicMethod.GetILGenerator();

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldarg_2);

                ilGenerator.Emit(OpCodes.Initblk);
                ilGenerator.Emit(OpCodes.Ret);

                set = (SetFunc)dynamicMethod.CreateDelegate(typeof(SetFunc));
            }

            public static void Load() { if (set == null) throw new Exception("Loading MemClear failed!"); }
        }

        public static void Clear(IntPtr dest, uint numBytes) { _Set.set(dest, 0, numBytes); }


        /// <summary>
        /// Does a raw byte copy from one pointer to another.  Completely unsafe.
        /// </summary>
        public static void Copy(IntPtr dest, IntPtr src, uint numBytes) { _Copy.copy(dest, src, numBytes); }
    }




}
