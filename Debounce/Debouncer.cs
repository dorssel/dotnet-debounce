using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("UnitTests")]
[assembly: InternalsVisibleTo("PerformanceTests")]

namespace Dorssel.Utility
{
    sealed class DebouncedEventArgs : EventArgs, IDebouncedEventArgs
    {
        public long Count { get; set; }
    }

    public sealed class Debouncer : IDebounce
    {
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
                };
            }
        }

        readonly object LockObject = new object();
        long Count = 0;
        readonly Stopwatch FirstTrigger = new Stopwatch();
        readonly Stopwatch LastTrigger = new Stopwatch();
        readonly Stopwatch LastHandlerFinished = new Stopwatch();
        readonly Timer Timer;
        bool TimerActive = false;
        bool SendingEvent = false;

        void OnTimer(object state)
        {
            lock (LockObject)
            {
                ++_Benchmark.TimerEvents;
                if (!IsDisposed)
                {
                    LockedReschedule();
                }
            }
        }

        void LockedReschedule()
        {
            Debug.Assert(!IsDisposed);

            ++_Benchmark.RescheduleCount;

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
                    Count += (Interlocked.Exchange(ref InterlockedCountMinusOne, -1) + 1);
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
                var sinceLastHandlerFinished = LastHandlerFinished.IsRunning ? LastHandlerFinished.Elapsed : TimeSpan.MaxValue;
                if (sinceLastHandlerFinished >= _BackoffInterval)
                {
                    // We are not within any backoff interval, so we may send an event if needed.
                    if ((sinceLastTrigger >= _DebounceInterval) || ((_DebounceTimeout != Timeout.InfiniteTimeSpan) && sinceFirstTrigger >= _DebounceTimeout))
                    {
                        var count = Count + (Interlocked.Exchange(ref InterlockedCountMinusOne, -1) + 1);

                        FirstTrigger.Reset();
                        LastTrigger.Reset();
                        Count = 0;
                        SendingEvent = true;
                        Task.Run(() =>
                        {
                            Debounced?.Invoke(this, new DebouncedEventArgs() { Count = count });
                            lock (LockObject)
                            {
                                _Benchmark.TriggersReported += count;
                                ++_Benchmark.HandlersCalled;
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
                dueTime = _DebounceInterval - sinceLastTrigger;
                // 2) Override with the debouncing timeout (if any) if that is sooner.
                if ((_DebounceTimeout != Timeout.InfiniteTimeSpan) && (dueTime > _DebounceTimeout - sinceFirstTrigger))
                {
                    dueTime = _DebounceTimeout - sinceFirstTrigger;
                }
                // 3) Override with the backoff interval if that is later.
                if (dueTime < _BackoffInterval - sinceLastHandlerFinished)
                {
                    dueTime = _BackoffInterval - sinceLastHandlerFinished;
                }
                // 4) Override with the timing granularity if we are currently coalescing triggers and if that is sooner.
                if ((countMinusOne >= 0) && (dueTime > _TimingGranularity))
                {
                    dueTime = _TimingGranularity;
                }
            }

            // Now set (or cancel) our timer.
            if (TimerActive || (dueTime != Timeout.InfiniteTimeSpan))
            {
                ++_Benchmark.TimerChanges;
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
        public event EventHandler<IDebouncedEventArgs>? Debounced;

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

        TimeSpan GetField(ref TimeSpan field)
        {
            lock (LockObject)
            {
                return field;
            }
        }

        void SetField(ref TimeSpan field, TimeSpan value, bool allowInfinite, Action? validate = null, [CallerMemberName] string fieldName = "")
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
                validate?.Invoke();
                field = value;
                LockedReschedule();
            }
        }

        TimeSpan _DebounceInterval = TimeSpan.Zero;
        public TimeSpan DebounceInterval
        {
            get => GetField(ref _DebounceInterval);
            set => SetField(ref _DebounceInterval, value, false, () =>
            {
                if (value < _TimingGranularity)
                {
                    throw new ArgumentException($"{nameof(DebounceInterval)} ({value}) must not be less than {nameof(TimingGranularity)} ({_TimingGranularity}).", nameof(DebounceInterval));
                }
                if ((_DebounceTimeout != Timeout.InfiniteTimeSpan) && (value > _DebounceTimeout))
                {
                    throw new ArgumentException($"{nameof(DebounceInterval)} ({value}) must not exceed {nameof(DebounceTimeout)} ({_DebounceTimeout}).", nameof(DebounceInterval));
                }
            });
        }

        TimeSpan _DebounceTimeout = Timeout.InfiniteTimeSpan;
        public TimeSpan DebounceTimeout
        {
            get => GetField(ref _DebounceTimeout);
            set => SetField(ref _DebounceTimeout, value, true, () =>
            {
                if ((value != Timeout.InfiniteTimeSpan) && (value < _DebounceInterval))
                {
                    throw new ArgumentException($"{nameof(DebounceTimeout)} ({value}) must not be less than {nameof(DebounceInterval)} ({_DebounceInterval}).", nameof(DebounceTimeout));
                }
            });
        }

        TimeSpan _BackoffInterval = TimeSpan.Zero;
        public TimeSpan BackoffInterval
        {
            get => GetField(ref _BackoffInterval);
            set => SetField(ref _BackoffInterval, value, false);
        }

        TimeSpan _TimingGranularity = TimeSpan.Zero;
        public TimeSpan TimingGranularity
        {
            get => GetField(ref _TimingGranularity);
            set => SetField(ref _TimingGranularity, value, false, () =>
            {
                if (value > _DebounceInterval)
                {
                    throw new ArgumentException($"{nameof(TimingGranularity)} ({value}) must not exceed {nameof(DebounceInterval)} ({_DebounceInterval}).", nameof(TimingGranularity));
                }
            });
        }
        #endregion

        #region IDisposable Support
        bool IsDisposed = false;

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref InterlockedCountMinusOne, long.MinValue) >= -1)
            {
                // not yet disposed
                lock (LockObject)
                {
                    IsDisposed = true;
                }
                Timer.Dispose();
            }
        }
        #endregion
    }
}
