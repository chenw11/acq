using System;
using System.Threading;

namespace Lab
{
    /// <summary>
    /// Encapsulates a disposable value which may be replaced with a new value
    /// constructed from a string
    /// </summary>
    public class SyncSwapValue<T> where T : class, IDisposable
    {
        /// <summary>
        /// Lock this when using the value
        /// </summary>
        public readonly object AccessLock = new object();

        private T value;

        /// <summary>
        /// Get a snapshot of the value.  Lock on AccessLock when using
        /// </summary>
        public T Value { get { return value; } }

        readonly Func<string, T> builder;

        public SyncSwapValue(Func<string, T> builder)
        {
            if (builder == null)
                throw new ArgumentNullException();
            this.builder = builder;
        }

        /// <summary>
        /// Swaps out the current value with a newly built one, and 
        /// disposes the old one.
        /// </summary>
        public void ReplaceWithNew(string creationString)
        {
            T oldValue = Interlocked.Exchange<T>(ref value, null);

            lock (AccessLock)
                oldValue.TryDispose();

            T newValue;
            if (string.IsNullOrWhiteSpace(creationString))
                newValue = null;
            else
                newValue = builder(creationString);

            Interlocked.Exchange<T>(ref value, newValue);
        }
    }
}
