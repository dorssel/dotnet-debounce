// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Dorssel.Utilities;

/// <summary>
/// This class implements the <see cref="IDebounce"/> interface.
/// </summary>
public sealed class Debouncer
    : IDebounce
    , IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer"/> class.
    /// </summary>
    public Debouncer()
    {
        Timer = new Timer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    long InterlockedCountMinusOne = -1;

    internal struct BenchmarkCounters
    {
        public long TriggersReported;
        public long HandlersCalled;
        public long RescheduleCount;
        public long TimerChanges;
        public long TimerEvents;
    }
    BenchmarkCounters _Benchmark;
    internal BenchmarkCounters Benchmark
    {
        get
        {
            lock (LockObject)
            {
                return _Benchmark;
            }
        }
    }

    readonly object LockObject = new();
    long Count;
    readonly Stopwatch FirstTrigger = new();
    readonly Stopwatch LastTrigger = new();
    readonly Stopwatch LastHandlerStarted = new();
    readonly Stopwatch LastHandlerFinished = new();
    readonly Timer Timer;
    bool TimerActive;
    bool SendingEvent;

    internal static long AddWithClamp(long left, long right)
    {
        Debug.Assert(left >= 0);
        Debug.Assert(right >= 0);

        return unchecked((long)Math.Min((ulong)left + (ulong)right, long.MaxValue));
    }

    void OnTimer(object? state)
    {
        lock (LockObject)
        {
            unchecked
            {
                ++_Benchmark.TimerEvents;
            }

            if (!IsDisposed)
            {
                LockedReschedule();
            }
        }
    }

    void LockedReschedule()
    {
        Debug.Assert(!IsDisposed);

        unchecked
        {
            ++_Benchmark.RescheduleCount;
        }

        var sinceFirstTrigger = FirstTrigger.Elapsed;
        var sinceLastTrigger = LastTrigger.Elapsed;

        var countMinusOne = Interlocked.Read(ref InterlockedCountMinusOne);
        if (countMinusOne >= 0)
        {
            // There are triggers that we have not yet processed.
            if (!FirstTrigger.IsRunning)
            {
                // Start coalescing triggers.
                FirstTrigger.Start();
                LastTrigger.Start();
            }
            if (sinceLastTrigger >= _TimingGranularity)
            {
                // Accumulate the coalesced triggers.
                Count = AddWithClamp(Count, Interlocked.Exchange(ref InterlockedCountMinusOne, -1) + 1);
                countMinusOne = -1;
                LastTrigger.Restart();
                sinceLastTrigger = TimeSpan.Zero;
            }
        }
        else if (Count == 0)
        {
            // No triggers, and nothing accumulated before: we are idle.
            return;
        }

        var dueTime = Timeout.InfiniteTimeSpan;
        if (SendingEvent)
        {
            // We are already sending an event, so we only need a timer if we are coalescing triggers.
            if (countMinusOne >= 0)
            {
                // We are coalescing triggers.
                dueTime = _TimingGranularity;
            }
        }
        else
        {
            // We are not currently sending an event, so check to see if we need to send one now.
            var sinceLastHandlerStarted = LastHandlerStarted.IsRunning ? LastHandlerStarted.Elapsed : TimeSpan.MaxValue;
            var sinceLastHandlerFinished = LastHandlerFinished.IsRunning ? LastHandlerFinished.Elapsed : TimeSpan.MaxValue;
            if ((sinceLastHandlerStarted >= _EventSpacing) && (sinceLastHandlerFinished >= _HandlerSpacing))
            {
                // We are not within any backoff interval, so we may send an event if needed.
                if ((sinceLastTrigger >= _DebounceWindow) || ((_DebounceTimeout != Timeout.InfiniteTimeSpan) && sinceFirstTrigger >= _DebounceTimeout))
                {
                    // Sending event now, so accumulate all coalesced triggers.
                    var count = AddWithClamp(Count, Interlocked.Exchange(ref InterlockedCountMinusOne, -1) + 1);

                    FirstTrigger.Reset();
                    LastTrigger.Reset();
                    Count = 0;
                    SendingEvent = true;
                    LastHandlerStarted.Restart();
                    // Must call handler asynchronously and outside the lock.
                    Task.Run(() =>
                    {
                        Debounced?.Invoke(this, new DebouncedEventArgs((long)count));
                        lock (LockObject)
                        {
                            // Handler has finished.
                            unchecked
                            {
                                _Benchmark.TriggersReported += count;
                                ++_Benchmark.HandlersCalled;
                            }
                            if (IsDisposed)
                            {
                                return;
                            }
                            LastHandlerFinished.Restart();
                            SendingEvent = false;
                            LockedReschedule();
                        }
                    });
                    return;
                }
            }

            // We are not sending an event, so wait until we should send it (with increasing priority):
            // 1) Start with the debouncing interval.
            dueTime = _DebounceWindow - sinceLastTrigger;
            // 2) Override with the debouncing timeout (if any) if that is sooner.
            if ((_DebounceTimeout != Timeout.InfiniteTimeSpan) && (dueTime > _DebounceTimeout - sinceFirstTrigger))
            {
                dueTime = _DebounceTimeout - sinceFirstTrigger;
            }
            // 3) Override with the event spacing if that is later.
            if (dueTime < _EventSpacing - sinceLastHandlerStarted)
            {
                dueTime = _EventSpacing - sinceLastHandlerStarted;
            }
            // 4) Override with the handler spacing if that is later.
            if (dueTime < _HandlerSpacing - sinceLastHandlerFinished)
            {
                dueTime = _HandlerSpacing - sinceLastHandlerFinished;
            }
            // 5) Override with the timing granularity if we are currently coalescing triggers and if that is sooner.
            if ((countMinusOne >= 0) && (dueTime > _TimingGranularity))
            {
                dueTime = _TimingGranularity;
            }
        }

        // Now set (or cancel) our timer.
        if (TimerActive || (dueTime != Timeout.InfiniteTimeSpan))
        {
            unchecked
            {
                ++_Benchmark.TimerChanges;
            }
            // System.Timer works with milliseconds, where -1 (== 2^32 - 1 == uint.MaxValue) means infinite
            // and 2^32 - 2 (or uint.MaxValue - 1) is the actual maximum.
            if (dueTime > TimeSpan.FromMilliseconds(uint.MaxValue - 1))
            {
                dueTime = TimeSpan.FromMilliseconds(uint.MaxValue - 1);
            }
            Timer.Change(dueTime, Timeout.InfiniteTimeSpan);
        }
        TimerActive = dueTime != Timeout.InfiniteTimeSpan;
    }

    #region IDebounce Support
    /// <inheritdoc/>
    public event EventHandler<DebouncedEventArgs>? Debounced;

    /// <inheritdoc/>
    public void Trigger()
    {
        var newCountMinusOne = Interlocked.Increment(ref InterlockedCountMinusOne);
        if (newCountMinusOne > 0)
        {
            // fast-path
            return;
        }
        else if (newCountMinusOne == 0)
        {
            // not-so-fast-path: This is not likely under stress.
            // The first trigger *must* Reschedule().
            lock (LockObject)
            {
                ThrowIfDisposed();
                LockedReschedule();
            }
        }
        else
        {
            // We were already disposed.
            Interlocked.Exchange(ref InterlockedCountMinusOne, long.MinValue);
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    /// <inheritdoc/>
    public long Reset()
    {
        lock (LockObject)
        {
            if (!IsDisposed)
            {
                Count = AddWithClamp(Count, Interlocked.Exchange(ref InterlockedCountMinusOne, -1) + 1);
            }
            var count = Count;
            Count = 0;
            if (!IsDisposed)
            {
                LockedReschedule();
            }
            return count;
        }
    }

    TimeSpan GetField(ref TimeSpan field)
    {
        lock (LockObject)
        {
            return field;
        }
    }

    void SetField(ref TimeSpan field, TimeSpan value, bool allowInfinite, [CallerMemberName] string fieldName = "")
    {
        lock (LockObject)
        {
            ThrowIfDisposed();
            if (field == value)
            {
                return;
            }
            if (value == Timeout.InfiniteTimeSpan)
            {
                if (!allowInfinite)
                {
                    throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} must not be infinite (-1 ms).");
                }
            }
            else if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} must not be negative.");
            }
            field = value;
            LockedReschedule();
        }
    }

    TimeSpan _DebounceWindow = TimeSpan.Zero;
    /// <inheritdoc/>
    public TimeSpan DebounceWindow
    {
        get => GetField(ref _DebounceWindow);
        set => SetField(ref _DebounceWindow, value, false);
    }

    TimeSpan _DebounceTimeout = Timeout.InfiniteTimeSpan;
    /// <inheritdoc/>
    public TimeSpan DebounceTimeout
    {
        get => GetField(ref _DebounceTimeout);
        set => SetField(ref _DebounceTimeout, value, true);
    }

    TimeSpan _EventSpacing = TimeSpan.Zero;
    /// <inheritdoc/>
    public TimeSpan EventSpacing
    {
        get => GetField(ref _EventSpacing);
        set => SetField(ref _EventSpacing, value, false);
    }

    TimeSpan _HandlerSpacing = TimeSpan.Zero;
    /// <inheritdoc/>
    public TimeSpan HandlerSpacing
    {
        get => GetField(ref _HandlerSpacing);
        set => SetField(ref _HandlerSpacing, value, false);
    }

    TimeSpan _TimingGranularity = TimeSpan.Zero;
    /// <inheritdoc/>
    public TimeSpan TimingGranularity
    {
        get => GetField(ref _TimingGranularity);
        set => SetField(ref _TimingGranularity, value, false);
    }
    #endregion

    #region IDisposable Support
    bool IsDisposed;

    void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    // NOTE: the additional summary is for docfx, which for some reason does not pick up the inheritdoc.
    /// <inheritdoc/>
    /// <summary>
    /// See <see cref="IDisposable.Dispose"/>
    /// </summary>
    public void Dispose()
    {
        lock (LockObject)
        {
            if (!IsDisposed)
            {
                Count = AddWithClamp(Count, Interlocked.Exchange(ref InterlockedCountMinusOne, long.MinValue) + 1);
                Timer.Dispose();
                IsDisposed = true;
            }
        }
    }
    #endregion
}
