using System;
using System.Threading;

/// <summary>
/// Base class for an IDisposable that prevents cleanup from executing more than once.
/// In other words, Dispose() is guaranteed idempotent.
/// </summary>
/// <remarks>
/// Cleanup is not guaranteed to be run, since this class doesn't implement a finalizer.
/// If your object is responsible for the cleanup of unmanaged resources 
/// (e.g. raw Thread objects) then use the DisposableFinalized instead.
/// </remarks>
[Serializable]
public abstract class Disposable : IDisposable
{
    int isDisposed = 0;

    public bool IsDisposed
    {
        get
        {   // non-cached read
            return Interlocked.CompareExchange(ref isDisposed, 0, 0) != 0;
        }
    }

    protected void AssertNotDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException("this");
    }

    /// <summary>
    /// Place cleanup logic in this method.  Do not call it yourself!
    /// </summary>
    /// <remarks>
    /// Cleanup any managed resources here (e.g. call Dispose on encapsulated methods,
    /// set to null any large fields).  
    /// </remarks>
    protected abstract void RunOnceDisposer();

    /// <summary>
    /// Disposes this object.  Can be called more than once, from different threads.
    /// It will call RunOnceDisposer only if it hasn't already been called.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref isDisposed, 1) == 0)
            RunOnceDisposer();
    }
}

/// <summary>
/// Provides extension methods for IDisposable and friends
/// </summary>
public static class DisposableExtensions
{
    /// <summary>
    /// If this is non-null, then calls dispose.
    /// </summary>
    /// <param name="d">IDisposable or null</param>
    public static void TryDispose(this IDisposable d)
    {
        if (d != null)
            d.Dispose();
    }
}
