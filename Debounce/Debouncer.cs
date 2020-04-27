using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("PerformanceTests")]

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
            TimingGranularityST = TimeSpanToStopwatchTicks(TimeSpan.FromMilliseconds(1));
        }

        public Debouncer(TimeSpan timingGranularity)
        {
            Timer = new Timer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            TimingGranularityST = TimeSpanToStopwatchTicks(timingGranularity, fieldName: nameof(timingGranularity));
        }

        static long TimeSpanToStopwatchTicks(TimeSpan value, bool allowZero = true, bool allowInfinite = false, Action? validate = null, string fieldName = "Internal.Error")
        {
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
            return ticks;
        }

        // ...ST means Stopwatch.Ticks, as provided by Stopwatch.GetTimestamp()

        readonly long TimingGranularityST;
        const long InfiniteTicks = -1;

        /// <summary>Interlocked</summary>
        long CountMinusOne = -1;
        /// <summary>Interlocked</summary>
        long LastRescheduleST = 0;

        /// <summary>PerformanceTests support</summary>
        internal long RescheduleCount = 0;

        readonly object LockObject = new object();
        long FirstTriggerST = 0;
        long LastTriggerST = 0;
        long LastHandlerFinishedST = 0;
        readonly Timer Timer;
        bool SendingEvent = false;

        void OnTimer(object state)
        {
            long countMinusOne;
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
                countMinusOne = Interlocked.Exchange(ref CountMinusOne, -1);
                if (countMinusOne < 0)
                {
                    // Either -1 (no event pending) or long.MinValue (already disposed).
                    return;
                }
                SendingEvent = true;
            }
            Debounced?.Invoke(this, new DebouncedEventArgs() { Count = countMinusOne + 1 });
            lock (LockObject)
            {
                if (_IsDisposed)
                {
                    return;
                }
                var now = Stopwatch.GetTimestamp();
                LastHandlerFinishedST = now;
                SendingEvent = false;
                LockedReschedule(now);
            }
        }

        void LockedReschedule(long now)
        {
            RescheduleCount++;

            if (SendingEvent || (Interlocked.Read(ref CountMinusOne) < 0))
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
            // This function is thread-safe and optimized for concurrent calls.
            // Optimization: prefer Interlocked over lock.

            // We *must* increment the count, so do it immediately.
            // A compare with 0 is fastest, hence CountMinusOne (instead of Count).
            long newCountMinusOne = Interlocked.Increment(ref CountMinusOne);
            if (newCountMinusOne > 0)
            {
                // fast-path: This is the most likely path under stress.
                // We are *not* the first to call Trigger() after an event. This means the timer is already running.
                // Let's find out if we can skip calling Reschedule().
                // NOTE: This is the reason why TimingGranularityST is a constant for this instance and cannot be
                // dynamically updated like the other timings; we would need another Interlocked.Read here.
                long lastReschedule = Interlocked.Read(ref LastRescheduleST);
                long now = Stopwatch.GetTimestamp();
                if (now - TimingGranularityST < lastReschedule)
                {
                    // fast-path: This is the most likely path under stress.
                    // No need to Reschedule() as it was already called recently (within the timing granularity window).
                    return;
                }
                // It appears we need to call Reschedule(), but other threads may think so too (race).
                if (Interlocked.CompareExchange(ref LastRescheduleST, now, lastReschedule) != lastReschedule)
                {
                    // fast-path: This is the most likely path under stress.
                    // We lost the race: another thread will call Reschedule(), so we won't.
                    return;
                }
                lock (LockObject)
                {
                    // not-so-fast-path: This is not likely under stress.
                    // Reschedule() must be called every now and then, and we get to do it.
                    ThrowIfDisposed();
                    LastTriggerST = now;
                    LockedReschedule(now);
                }
            }
            else if (newCountMinusOne == 0)
            {
                // not-so-fast-path: This is not likely under stress.
                // The first trigger after an event *must* Reschedule().
                // Update LastRescheduleST as soon as possible to prevent other threads to also call Reschedule().
                long now = Stopwatch.GetTimestamp();
                Interlocked.Exchange(ref LastRescheduleST, now);
                lock (LockObject)
                {
                    // Reschedule() must be called every now and then, and we get to do it.
                    ThrowIfDisposed();
                    FirstTriggerST = now;
                    LastTriggerST = now;
                    LockedReschedule(now);
                }
            }
            else
            {
                // We were already disposed.
                Interlocked.CompareExchange(ref CountMinusOne, long.MinValue, newCountMinusOne);
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

        void SetField(ref TimeSpan field, ref long tickField, TimeSpan value, bool allowZero, bool allowInfinite, Action? validate = null, [CallerMemberName] string fieldName = "")
        {
            lock (LockObject)
            {
                ThrowIfDisposed();
                if (field == value)
                {
                    return;
                }
                long ticks = TimeSpanToStopwatchTicks(value, allowZero, allowInfinite, validate, fieldName);
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
#endregion

#region IDisposable Support
        bool _IsDisposed = false;

        void ThrowIfDisposed()
        {
            if (_IsDisposed)
            {
                Interlocked.Exchange(ref CountMinusOne, long.MinValue);
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
                Interlocked.Exchange(ref CountMinusOne, long.MinValue);
                _IsDisposed = true;
            }
            Timer.Dispose();
        }
#endregion
    }
}
