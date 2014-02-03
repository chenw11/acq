using System;
using System.Collections.Concurrent;

using System.Threading;

namespace Lab.Acq
{
    /// <summary>
    /// A simple fixed-size circular buffer that doesn't lock, suitable for 
    /// temporary buffering of live data sources
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RingBuffer<T> : IRingBufferWrite<T> where T:class 
    {
        readonly ConcurrentQueue<T> usable = new ConcurrentQueue<T>();
        readonly ConcurrentQueue<T> unread = new ConcurrentQueue<T>();
        readonly AutoResetEvent newUnread = new AutoResetEvent(false);

        int nFailedWrites = 0;

        /// <summary>
        /// Returns a snapshot of the counter which increments every time a write fails for lack of available buffer space
        /// </summary>
        public int NumFailedWrites { get { return nFailedWrites; } }

        /// <summary>
        /// Returns the number of failed writes which have occurred since the last call, and resets the counter.
        /// Failed writes occur when there isn't a free buffer slot.
        /// </summary>
        /// <returns>Number of failed writes since the last call</returns>
        public int ResetNumFailedWrites() { return Interlocked.Exchange(ref nFailedWrites, 0); }

        int nMissedReads;

        public int NumMissedReads { get { return nMissedReads; } }

        public int ResetNumMissedReads() { return Interlocked.Exchange(ref nMissedReads, 0); }


        readonly Func<T> bufferFactory;

        public void IncreaseBufferCapacity(int nExtraSlots)
        {
            if (nExtraSlots < 1)
                throw new ArgumentOutOfRangeException();

            for(int i=0; i<nExtraSlots; i++)
                usable.Enqueue(bufferFactory());
        }

        /// <summary>
        /// When true, new writes may overwrite old, unread slots if there
        /// is no remaining slack in the queue.  Each overwrite increments
        /// NumMissedReads.
        /// When false, old data is preserved and new writes fail.  Each
        /// failed write increments NumFailedWrites.
        /// </summary>
        public bool KeepFresh { get; set; }

        public RingBuffer(int capacity, Func<T> bufferFactory)
        {
            if (capacity < 2)
                throw new ArgumentOutOfRangeException();
            if (bufferFactory == null)
                throw new ArgumentOutOfRangeException();
            this.bufferFactory = bufferFactory;

            IncreaseBufferCapacity(capacity);
        }



        public T RemoveBufferForWrite()
        {
            T buf;
            if (usable.TryDequeue(out buf))
                return buf;
            else
            {
                if (KeepFresh && unread.TryDequeue(out buf))
                {
                    Interlocked.Increment(ref nMissedReads);
                    return buf;
                }
                else
                {
                    Interlocked.Increment(ref nFailedWrites);
                    return null;
                }
            }
        }

        public void Write(T data)
        {
            if (data == null)
                throw new ArgumentNullException();
            unread.Enqueue(data);
            newUnread.Set();
        }


        /// <summary>
        /// Tries to read a buffer.  Does not block.
        /// </summary>
        /// <param name="reader">Action to perform on buffer that is read</param>
        /// <returns>True if buffer was successfully read</returns>
        public bool TryRead(Action<T> reader)
        {
            T buf;
            if (unread.TryDequeue(out buf))
            {
                try
                {
                    reader(buf);
                }
                finally
                {
                    usable.Enqueue(buf);
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Blocks until a buffer can be read.
        /// </summary>
        /// <param name="reader">Action to perform on buffer that is read</param>
        /// <param name="cancel">Cancelation token</param>
        /// <returns>True if data was read, false if canceled</returns>
        public bool Read(Action<T> reader, CancellationToken cancel)
        {
            while (true)
            {
                if (TryRead(reader))
                    return true;
                if (cancel.IsCancellationRequested)
                    return false;
                newUnread.WaitOne(100);
            }
        }
    }
}
