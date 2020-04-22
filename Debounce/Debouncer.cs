using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dorssel.Utility
{
    sealed class DebouncedEventArgs : EventArgs, IDebouncedEventArgs
    {
        public long Count { get; set; }
        public DateTimeOffset FirstTrigger { get; set; }
        public DateTimeOffset LastTrigger { get; set; }
    }

    public sealed class Debouncer : IDebounce
    {
        public Debouncer()
        {
            Timer = new Timer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            TimingGranularity = TimeSpan.FromMilliseconds(1);
        }

        // ...ST means Stopwatch.Ticks, as provided by Stopwatch.GetTimestamp()

        const long InfiniteTicks = -1;

        readonly object LockObject = new object();
        long Count = 0;
        long LastRescheduleST = 0;
        long FirstTriggerST = 0;
        long LastTriggerST = 0;
        long LastHandlerFinishedST = 0;
        readonly Timer Timer;
        bool SendingEvent = false;

        void OnTimer(object state)
        {
            long count;
            lock (LockObject)
            {
                if (SendingEvent)
                {
                    // We need to invoke any handlers outside the lock. There is a race between
                    // deciding to send here, and a reconfigure of one of the timings followed by a
                    // trigger. That can lead to setting and tripping the timer again before we finish
                    // sending. Hence, we need to guard against being called concurrently multiple times.
                    return;
                }
                count = Interlocked.Exchange(ref Count, 0);
                if (count == 0)
                {
                    return;
                }
                SendingEvent = true;
            }
            Debounced?.Invoke(this, new DebouncedEventArgs() { Count = count });
            lock (LockObject)
            {
                var now = Stopwatch.GetTimestamp();
                LastHandlerFinishedST = now;
                SendingEvent = false;
                LockedReschedule(now);
            }
        }

        void LockedReschedule(long now)
        {
            if (SendingEvent || (Count == 0))
            {
                // Nothing to schedule at this time.
                return;
            }

            // Calculate the new timer delay (in Stopwatch ticks first).
            // 1) Wait for another debounce interval since the last trigger.
            var ticks = LastTriggerST - now + DebounceIntervalST;
            // 2) Maximize it to the debounce timeout since the first trigger.
            if (DebounceTimeoutST != InfiniteTicks)
            {
                var timeout = FirstTriggerST - now + DebounceTimeoutST;
                if (ticks > timeout)
                {
                    ticks = timeout;
                }
            }
            // 3) Minimize it to the backoff interval since the last handler was called.
            if (LastHandlerFinishedST > 0)
            {
                var backoff = LastHandlerFinishedST - now + BackoffIntervalST;
                if (ticks < backoff)
                {
                    ticks = backoff;
                }
            }
            // 4) Sanitize ... we cannot go back in time.
            if (ticks < 0)
            {
                ticks = 0;
            }

            Timer.Change(TimeSpan.FromSeconds((double)ticks / Stopwatch.Frequency), Timeout.InfiniteTimeSpan);
        }

        #region IDebounce Support
        public event EventHandler<IDebouncedEventArgs>? Debounced;

        public void Trigger()
        {
            if (Interlocked.Increment(ref Count) == 1)
            {
                lock (LockObject)
                {
                    ThrowIfDisposed();
                    long now = Stopwatch.GetTimestamp();
                    FirstTriggerST = now;
                    LastTriggerST = now;
                    LockedReschedule(now);
                }
            }
            else
            {
                long lastReschedule = Interlocked.Read(ref LastRescheduleST);
                long now = Stopwatch.GetTimestamp();
                if (now > lastReschedule + TimingGranularityST)
                {
                    if (Interlocked.CompareExchange(ref LastRescheduleST, now, lastReschedule) == lastReschedule)
                    {
                        lock (LockObject)
                        {
                            ThrowIfDisposed();
                            LastTriggerST = now;
                            LockedReschedule(now);
                        }
                    }
                }
            }
        }

        TimeSpan GetField(ref TimeSpan field)
        {
            lock (LockObject)
            {
                return field;
            }
        }

        void SetField(ref TimeSpan field, ref long tickField, TimeSpan value, bool allowZero, bool allowInfinite, Action? validate = null, [CallerMemberName] string fieldName = "")
        {
            lock (LockObject)
            {
                ThrowIfDisposed();
                if (field == value)
                {
                    return;
                }
                long ticks;
                if (value == Timeout.InfiniteTimeSpan)
                {
                    if (!allowInfinite)
                    {
                        throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} must not be infinite (-1 ms).");
                    }
                    ticks = InfiniteTicks;
                }
                else if (value == TimeSpan.Zero)
                {
                    if (!allowZero)
                    {
                        throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} must not be zero.");
                    }
                    ticks = 0;
                }
                else if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} must not be negative.");
                }
                else
                {
                    // guard against overflow
                    decimal x = (decimal)value.TotalSeconds * Stopwatch.Frequency;
                    ticks = (x > long.MaxValue) ? long.MaxValue : (long)x;
                    if (ticks <= 0)
                    {
                        // map any TimeSpan > Zero to at least one tick
                        ticks = 1;
                    }
                }
                validate?.Invoke();
                field = value;
                if (tickField == ticks)
                {
                    return;
                }
                tickField = ticks;
                LockedReschedule(Stopwatch.GetTimestamp());
            }
        }

        long DebounceIntervalST = 0;
        TimeSpan _DebounceInterval = TimeSpan.Zero;
        public TimeSpan DebounceInterval
        {
            get => GetField(ref _DebounceInterval);
            set => SetField(ref _DebounceInterval, ref DebounceIntervalST, value, true, false, () =>
            {
                if ((_DebounceTimeout != Timeout.InfiniteTimeSpan) && (value > _DebounceTimeout))
                {
                    throw new ArgumentException($"{nameof(DebounceInterval)} ({value}) must not exceed {nameof(DebounceTimeout)} ({DebounceTimeout}).", nameof(DebounceInterval));
                }
            });
        }

        long DebounceTimeoutST = InfiniteTicks;
        TimeSpan _DebounceTimeout = Timeout.InfiniteTimeSpan;
        public TimeSpan DebounceTimeout
        {
            get => GetField(ref _DebounceTimeout);
            set => SetField(ref _DebounceTimeout, ref DebounceTimeoutST, value, true, true, () =>
            {
                if ((value != Timeout.InfiniteTimeSpan) && (value < _DebounceInterval))
                {
                    throw new ArgumentException($"{nameof(DebounceTimeout)} ({value}) must not be less than {nameof(DebounceInterval)} ({DebounceInterval}).", nameof(DebounceTimeout));
                }
            });
        }

        long BackoffIntervalST = 0;
        TimeSpan _BackoffInterval = TimeSpan.Zero;
        public TimeSpan BackoffInterval
        {
            get => GetField(ref _BackoffInterval);
            set => SetField(ref _BackoffInterval, ref BackoffIntervalST, value, true, false);
        }

        long TimingGranularityST = 0;
        TimeSpan _TimingGranularity = TimeSpan.Zero;
        public TimeSpan TimingGranularity
        {
            get => GetField(ref _TimingGranularity);
            set => SetField(ref _TimingGranularity, ref TimingGranularityST, value, true, false);
        }
        #endregion

        #region IDisposable Support
        volatile bool _IsDisposed = false;

        void ThrowIfDisposed()
        {
            if (_IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public void Dispose()
        {
            lock (LockObject)
            {
                if (_IsDisposed)
                {
                    return;
                }
                _IsDisposed = true;
            }
            Timer.Dispose();
        }
#endregion
    }
}
