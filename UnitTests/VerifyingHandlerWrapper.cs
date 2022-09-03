// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace UnitTests;

sealed class VerifyingHandlerWrapper : IDisposable
{
    public VerifyingHandlerWrapper(IDebounce debouncer)
    {
        Debouncer = debouncer;
        Debouncer.Debounced += OnDebounced;
    }

    public event EventHandler<DebouncedEventArgs>? Debounced;

    public long HandlerCount { get; private set; }
    public long TriggerCount { get; private set; }

    void OnDebounced(object? sender, DebouncedEventArgs debouncedEventArgs)
    {
        // sender *must* be the original debouncer object
        Assert.AreSame(Debouncer, sender);
        // *must* have a positive trigger count since last handler called
        Assert.IsTrue(debouncedEventArgs.Count > 0);
        // *never* should be called reentrant (i.e. always serialize handlers)
        Assert.AreEqual(Interlocked.Increment(ref ReentrancyCount), 1);

        ++HandlerCount;
        TriggerCount += debouncedEventArgs.Count;

        Debounced?.Invoke(this, debouncedEventArgs);

        // *never* should be called reentrant (i.e. always serialize handlers)
        Assert.AreEqual(Interlocked.Decrement(ref ReentrancyCount), 0);
    }

    readonly IDebounce Debouncer;
    int ReentrancyCount;

    #region IDisposable Support
    int IsDisposed;

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref IsDisposed, 1, 0) == 0)
        {
            Debouncer.Debounced -= OnDebounced;
        }
    }
    #endregion
}
