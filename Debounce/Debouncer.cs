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

        /// <summary>Test support</summary>
        internal long RescheduleCount = 0;
        /// <summary>Test support</summary>
        internal long TimerChanges = 0;
        /// <summary>Test support</summary>
        internal long TimerEvents = 0;

        readonly object LockObject = new object();
        long Count = 0;
        readonly Stopwatch FirstTrigger = new Stopwatch();
        readonly Stopwatch LastTrigger = new Stopwatch();
        readonly Stopwatch LastHandlerFinished = new Stopwatch();
        readonly Timer Timer;
        bool SendingEvent = false;

        void OnTimer(object state)
        {
            lock (LockObject)
            {
                ++TimerEvents;
                if (!IsDisposed)
                {
                    LockedReschedule();
                }
            }
        }

        void LockedHandleNow()
        {
            Debug.Assert(!SendingEvent);
            Debug.Assert(!IsDisposed);

            long count = Count + (Interlocked.Exchange(ref InterlockedCountMinusOne, -1) + 1);
            Debug.Assert(count > 0);

            FirstTrigger.Reset();
            LastTrigger.Reset();
            Count = 0;
            SendingEvent = true;
            Task.Run(() =>
            {
                Debounced?.Invoke(this, new DebouncedEventArgs() { Count = count });
                lock (LockObject)
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    LastHandlerFinished.Restart();
                    SendingEvent = false;
                    LockedReschedule();
                }
            });
        }

        void LockedReschedule()
        {
            Debug.Assert(!IsDisposed);

            RescheduleCount++;

            long countMinusOne = Interlocked.Read(ref InterlockedCountMinusOne);
            if ((countMinusOne >= 0) && !FirstTrigger.IsRunning)
            {
                FirstTrigger.Restart();
            }
            long totalCount = Count + (countMinusOne + 1);
            if (totalCount == 0)
            {
                return;
            }

            LastTrigger.Start();

            if (SendingEvent)
            {
                return;
            }

            if ((_BackoffInterval > TimeSpan.Zero) && LastHandlerFinished.IsRunning)
            {
                var sinceLastHandlerFinished = LastHandlerFinished.Elapsed;
                if (_BackoffInterval > sinceLastHandlerFinished)
                {
                    ++TimerChanges;
                    Timer.Change(_BackoffInterval - sinceLastHandlerFinished, Timeout.InfiniteTimeSpan);
                    return;
                }
            }

            var sinceFirstTrigger = FirstTrigger.Elapsed;
            var sinceLastTrigger = LastTrigger.Elapsed;

            if (sinceLastTrigger >= _DebounceInterval)
            {
                LockedHandleNow();
                return;
            }
            if ((_DebounceTimeout != Timeout.InfiniteTimeSpan) && sinceFirstTrigger >= _DebounceTimeout)
            {
                LockedHandleNow();
                return;
            }

            if (sinceLastTrigger > _TimingGranularity)
            {
                countMinusOne = Interlocked.Exchange(ref InterlockedCountMinusOne, -1);
                if (countMinusOne >= 0)
                {
                    Count += (countMinusOne + 1);
                    LastTrigger.Restart();
                }
            }
            ++TimerChanges;
            Timer.Change(_TimingGranularity, Timeout.InfiniteTimeSpan);
        }

        #region IDebounce Support
        public event EventHandler<IDebouncedEventArgs>? Debounced;

        public void Trigger()
        {
            long newCountMinusOne = Interlocked.Increment(ref InterlockedCountMinusOne);
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
                Interlocked.Exchange(ref InterlockedCountMinusOne, long.MinValue);
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref InterlockedCountMinusOne, long.MinValue);
            lock (LockObject)
            {
                if (IsDisposed)
                {
                    return;
                }
                FirstTrigger.Reset();
                LastTrigger.Reset();
                LastHandlerFinished.Reset();
                IsDisposed = true;
            }
            Timer.Dispose();
        }
#endregion
    }
}
