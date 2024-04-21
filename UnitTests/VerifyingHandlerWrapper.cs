// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace UnitTests;

sealed class VerifyingHandlerWrapper<TData> : IDisposable
{
    public VerifyingHandlerWrapper(IDebouncer<TData> debouncer)
    {
        Debouncer = debouncer;
        Debouncer.Debounced += OnDebounced;
    }

    public event EventHandler<DebouncedEventArgs<TData>>? Debounced;

    public long HandlerCount { get; private set; }
    public long TriggerCount { get; private set; }

    /// <summary>
    /// TriggerData of the last Debounced call.
    /// </summary>
    public IReadOnlyList<TData> LastTriggerData { get; private set; } = [];

    readonly List<TData> _TriggerData = [];
    /// <summary>
    /// Concatenation of all TriggerData from all Debounced calls.
    /// </summary>
    public IReadOnlyList<TData> TriggerData { get => _TriggerData; }

    void OnDebounced(object? sender, DebouncedEventArgs<TData> debouncedEventArgs)
    {
        // sender *must* be the original debouncer object
        Assert.AreSame(Debouncer, sender);
        // *must* have a positive trigger count since last handler called
        Assert.IsTrue(debouncedEventArgs.Count > 0);
        // *never* should be called reentrant (i.e. always serialize handlers)
        Assert.AreEqual(Interlocked.Increment(ref ReentrancyCount), 1);

        ++HandlerCount;
        TriggerCount += debouncedEventArgs.Count;
        LastTriggerData = debouncedEventArgs.TriggerData;
        _TriggerData.AddRange(debouncedEventArgs.TriggerData);

        Debounced?.Invoke(this, debouncedEventArgs);

        // *never* should be called reentrant (i.e. always serialize handlers)
        Assert.AreEqual(Interlocked.Decrement(ref ReentrancyCount), 0);
    }

    readonly IDebouncer<TData> Debouncer;
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
