using System;
using System.Collections.Generic;

namespace Lab
{
    public static class Utilities
    {
        /// <summary>
        /// Integer division that rounds up.
        /// </summary>
        /// <param name="dividend">Number to divide (numerator)</param>
        /// <param name="divisor">Number to divide by (denominator)</param>
        /// <returns>Quotient</returns>
        /// <remarks>Credit to Eric Lippert: http://ericlippert.com/2013/01/28/integer-division-that-rounds-up/ </remarks>
        public static int Divide_RoundUp(int dividend, int divisor)
        {
            if (divisor == 0)
                throw new DivideByZeroException();
            if (divisor == -1 && dividend == Int32.MinValue)
                throw new OverflowException();

            int roundedTowardsZeroQuotient = dividend / divisor;
            bool dividedEvenly = (dividend % divisor) == 0;

            if (dividedEvenly)
                return roundedTowardsZeroQuotient;
            
            bool wasRoundedDown = ((divisor > 0) == (dividend > 0));
            if (wasRoundedDown)
                return roundedTowardsZeroQuotient + 1;
            else
                return roundedTowardsZeroQuotient;
        }

        public static int BitsToBytes(int nBits)
        {
            return Divide_RoundUp(nBits, 8);
        }

        public static int BytesToBits(int nBytes)
        {
            return nBytes * 8;
        }


        /// <summary>
        /// Copies data from the given source pointer to the given destinatin buffer.  Source can be unmanaged.
        /// </summary>
        unsafe public static void CopyPointerToBuffer(IntPtr source, byte[] destBuffer, uint destOffset, uint nBytes)
        {
            if (source == IntPtr.Zero)
                throw new ArgumentException("Null pointer for source");
            if (destBuffer == null)
                throw new ArgumentNullException();
            if (checked(nBytes + destOffset > destBuffer.Length))
                throw new ArgumentException();

            fixed (byte* pDest = destBuffer)
                Mem.Copy((IntPtr)pDest, source, nBytes);
        }

        /// <summary>
        /// Folds a function over the tree of interfaces 
        /// </summary>
        public static IEnumerable<T> RecurseInterfaces<T>(Type iface, Func<Type, IEnumerable<T>> f)
        {
            foreach (var x in f(iface))
                yield return x;

            foreach (Type i in iface.GetInterfaces())
                foreach (var y in RecurseInterfaces(i, f))
                    yield return y;
        }


        public static string FriendlyNameForGenerics(this Type t)
        {
            var g = t.GetGenericArguments();
            if (g.Length == 0)
                return t.Name;
            else
                return t.Name.Replace("`" + g.Length,
                    "<" + string.Join(",", Array.ConvertAll(g, x => x.Name)) + ">");
        }


    }
}
