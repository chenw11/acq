using System;

namespace Lab.Acq
{
    public static class RingBufferExtensions
    {
        /// <summary>
        /// Attempts to copy data into the ring buffer using the specified copier action.
        /// Robust against this == null, and against buffer having no free slots
        /// </summary>
        /// <typeparam name="TIn">Type of data to copy into buffer</typeparam>
        /// <typeparam name="TRB">Type of data stored in buffer</typeparam>
        /// <param name="ringBuffer">Ring buffer to hold data</param>
        /// <param name="copier">Action to copy from input format into ring buffer slot</param>
        /// <param name="inputData">Input data</param>
        /// <returns>True if copy succeeded; false otherwise (no available buffer slots, or this buffer is null)</returns>
        public static bool TryCopyIn<TIn, TRB>(this IRingBufferWrite<TRB> ringBuffer,
            Action<TIn, TRB> copier, TIn inputData)
            where TRB:class
        {
            if (ringBuffer == null)
                return false;

            TRB temp = ringBuffer.RemoveBufferForWrite();
            if (temp == null)
                return false;

            copier(inputData, temp);
            ringBuffer.Write(temp);
            return true;          
        }
    }
}
