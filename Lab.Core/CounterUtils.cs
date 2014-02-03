using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab
{
    public static class CounterUtils
    {
        /// <summary>
        /// Returns the maximum allowable value for a counter of the given bitness
        /// </summary>
        /// <param name="counterBitness">Between 2 and 30</param>
        /// <returns>The maximum allowable value</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static int GetMaxValueForCounter(int counterBitness)
        {
            if ((counterBitness < 2) || (counterBitness > 30))
                throw new ArgumentOutOfRangeException("counterBitness");
            return (1 << counterBitness) - 1;
        }


        /// <summary>
        /// Attempts to unwrap a reading from a unsigned counter that rolled over
        /// </summary>
        /// <param name="counterBits">Number of bits used by the rolling counter (log2 of its range).  Between 2 and 30</param>
        /// <param name="prevValue">Previous value.  Non-negative, but may exceed counter range</param>
        /// <param name="newRead">New value read from the counter Non-negative, less than counter range</param>
        /// <returns>A new value that may exceed the counter range</returns>
        /// <remarks>
        /// This function assumes that the counter increases between reads are less than the counter range (2 ^ bitness)
        /// </remarks>
        public static uint UnwrapRolledCounter(byte counterBits, uint prevValue, uint newRead)
        {
            if ((counterBits < 2) || (counterBits > 30))
                throw new ArgumentOutOfRangeException("counterBits", "expecting counterBits between 2 and 30");

            uint counterRange = (uint)(1 << counterBits);
            if ((newRead < 0) || (newRead >= counterRange))
                throw new ArgumentOutOfRangeException("newRead", "expecting newRead to be non-negative but less than counter range " + counterRange);

            if (newRead >= prevValue)
                return newRead;
            else
                return prevValue + newRead + (counterRange - (prevValue % counterRange));
        }

    }
}
