// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

using System.Collections.ObjectModel;

namespace Dorssel.Utilities.Generic;

/// <summary>
/// Object which debounces events, i.e., accumulating multiple incoming events into one with the possibility of 
/// keeping track of the incoming trigger data.
/// </summary>
public sealed class Debouncer<TData> : DebouncerBase<DebouncedEventArgs<TData>>, IDebouncer<TData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer{TData}"/> class.
    /// </summary>
    public Debouncer()
        : base(TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer{TData}"/> class using the specified <see cref="TimeProvider"/>.
    /// </summary>
    /// <remarks>This constructor is intended for unit testing.</remarks>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to use.</param>
    public Debouncer(TimeProvider timeProvider)
        : base(timeProvider)
    {
    }

    static readonly IReadOnlyList<TData> EmptyTriggerData = new ReadOnlyCollection<TData>([]);
    List<TData> TriggerData = [];

    /// <inheritdoc/>
    private protected override DebouncedEventArgs<TData> LockedCreateEventArgs(long count)
    {
        return new(count, LockedExchangeTriggerData());
    }

    #region IDebouncer<TData> Support

    int _DataLimit = int.MaxValue;

    /// <inheritdoc/>
    /// <remarks>
    /// The default value is <see cref="int.MaxValue"/>.
    /// </remarks>
    public int DataLimit
    {
        get
        {
            lock (LockObject)
            {
                return _DataLimit;
            }
        }
        set
        {
            lock (LockObject)
            {
                ThrowIfDisposed();
                if (_DataLimit == value)
                {
                    return;
                }
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(DataLimit), $"{nameof(DataLimit)} must be greater than zero.");
                }
                _DataLimit = value;
                if (TriggerData.Count >= _DataLimit)
                {
                    LockedSendEvent();
                }
            }
        }
    }

    /// <inheritdoc />
    public void Trigger(TData data)
    {
        lock (LockObject)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (TriggerData.Count >= DataLimit)
            {
                throw new InvalidOperationException("Trigger data reached limit while already executing event handler.");
            }
            // Must add data first, as Trigger() may decide to invoke the Debounced event including the current trigger.
            TriggerData.Add(data);
            Trigger();
            // Additional event reason
            if (!SendingEvent && (TriggerData.Count >= _DataLimit))
            {
                LockedSendEvent();
            }
        }
    }

    IReadOnlyList<TData> LockedExchangeTriggerData()
    {
        if (TriggerData.Count == 0)
        {
            // Prevent unnecessary reallocations.
            return EmptyTriggerData;
        }
        else
        {
            return new ReadOnlyCollection<TData>(Interlocked.Exchange(ref TriggerData, []));
        }
    }

    /// <inheritdoc/>
    public long Reset(out IReadOnlyList<TData> data)
    {
        lock (LockObject)
        {
            data = LockedExchangeTriggerData();
            return Reset();
        }
    }

    private protected override void LockedReset()
    {
        _ = LockedExchangeTriggerData();
    }

    #endregion
}
