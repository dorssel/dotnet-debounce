using System;
using System.Diagnostics;
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
        }

        readonly object LockObject = new object();
        DebouncedEventArgs EventArgs = new DebouncedEventArgs();
        readonly Stopwatch FirstTriggerStopwatch = new Stopwatch();
        readonly Stopwatch LastTriggerStopwatch = new Stopwatch();
        readonly Stopwatch LastHandlerFinished = new Stopwatch();
        readonly Timer Timer;
        bool SendingEvent = false;

        void OnTimer(object state)
        {
            var eventArgs = new DebouncedEventArgs();
            bool sendNow = false;
            lock (LockObject)
            {
                // We need to invoke any handlers outside the lock. There can be a race between
                // deciding to send here, and a reconfigure of one of the timings followed by a
                // trigger. That can lead to setting and tripping the timer again before we finish
                // sending. Hence, we need to guard against being called concurrently multiple times.
                sendNow = !SendingEvent && (EventArgs.Count > 0);
                if (sendNow)
                {
                    var temp = EventArgs;
                    EventArgs = eventArgs;
                    eventArgs = temp;
                    SendingEvent = true;
                }
            }
            if (sendNow)
            {
                // We won the race.
                Debounced?.Invoke(this, eventArgs);
                lock (LockObject)
                {
                    LastHandlerFinished.Restart();
                    SendingEvent = false;
                    LockedReschedule();
                }
            }
        }

        #region IDebounce Support
        public event EventHandler<IDebouncedEventArgs>? Debounced;

        public void Trigger()
        {
            lock (LockObject)
            {
                LockedThrowIfDisposed();
                var now = DateTimeOffset.Now;
                if (EventArgs.Count++ == 0)
                {
                    EventArgs.FirstTrigger = now;
                    FirstTriggerStopwatch.Restart();
                }
                EventArgs.LastTrigger = now;
                LastTriggerStopwatch.Restart();
                LockedReschedule();
            }
        }

        void LockedReschedule()
        {
            if (SendingEvent || (EventArgs.Count == 0))
            {
                // Nothing to schedule at this time.
                return;
            }

            // Calculate the new timer delay.
            // 1) Wait for another debounce interval since the last trigger.
            var timeSpan = DebounceInterval.Subtract(LastTriggerStopwatch.Elapsed);
            // 2) Maximize it to the debounce timeout since the first trigger.
            if (DebounceTimeout != Timeout.InfiniteTimeSpan)
            {
                var timeout = DebounceTimeout.Subtract(FirstTriggerStopwatch.Elapsed);
                if (timeSpan.CompareTo(timeout) > 0)
                {
                    timeSpan = timeout;
                }
            }
            // 3) Minimize it to the backoff interval since the last handler was called.
            if (LastHandlerFinished.IsRunning)
            {
                var backoff = BackoffInterval.Subtract(LastHandlerFinished.Elapsed);
                if (timeSpan.CompareTo(backoff) < 0)
                {
                    timeSpan = backoff;
                }
            }
            // 4) Sanitize ... we cannot go back in time.
            if (timeSpan.CompareTo(TimeSpan.Zero) < 0)
            {
                timeSpan = TimeSpan.Zero;
            }

            Timer.Change(timeSpan, Timeout.InfiniteTimeSpan);
        }

        T LockedGet<T>(ref T field) where T : struct
        {
            lock (LockObject)
            {
                return field;
            }
        }

        TimeSpan _LockedDebounceInterval = TimeSpan.Zero;
        public TimeSpan DebounceInterval
        {
            get => LockedGet(ref _LockedDebounceInterval);
            set
            {
                if (value.CompareTo(TimeSpan.Zero) < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(DebounceInterval), $"{nameof(DebounceInterval)} must be non-negative.");
                }
                lock (LockObject)
                {
                    LockedThrowIfDisposed();
                    if (_LockedDebounceInterval == value)
                    {
                        return;
                    }
                    if ((_LockedDebounceTimeout != Timeout.InfiniteTimeSpan) && (value.CompareTo(_LockedDebounceTimeout) > 0))
                    {
                        throw new ArgumentException($"{nameof(DebounceInterval)} ({value}) must not exceed {nameof(DebounceTimeout)} ({DebounceTimeout}).", nameof(DebounceInterval));
                    }
                    _LockedDebounceInterval = value;
                    LockedReschedule();
                }
            }
        }

        TimeSpan _LockedDebounceTimeout = Timeout.InfiniteTimeSpan;
        public TimeSpan DebounceTimeout
        {
            get => LockedGet(ref _LockedDebounceTimeout);
            set
            {
                if ((value != Timeout.InfiniteTimeSpan) && (value.CompareTo(TimeSpan.Zero) < 0))
                {
                    throw new ArgumentOutOfRangeException(nameof(DebounceTimeout));
                }
                lock (LockObject)
                {
                    LockedThrowIfDisposed();
                    if (_LockedDebounceTimeout == value)
                    {
                        return;
                    }
                    if ((value != Timeout.InfiniteTimeSpan) && (value.CompareTo(_LockedDebounceInterval) < 0))
                    {
                        throw new ArgumentException($"{nameof(DebounceTimeout)} ({value}) must not be less than {nameof(DebounceInterval)} ({DebounceInterval}).", nameof(DebounceTimeout));
                    }
                    _LockedDebounceTimeout = value;
                    LockedReschedule();
                }
            }
        }

        TimeSpan _LockedBackoffInterval = TimeSpan.Zero;
        public TimeSpan BackoffInterval
        {
            get => LockedGet(ref _LockedBackoffInterval);
            set
            {
                if (value.CompareTo(TimeSpan.Zero) < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(BackoffInterval), $"{nameof(BackoffInterval)} must be non-negative.");
                }
                lock (LockObject)
                {
                    LockedThrowIfDisposed();
                    if (_LockedBackoffInterval == value)
                    {
                        return;
                    }
                    _LockedBackoffInterval = value;
                    LockedReschedule();
                }
            }
        }
        #endregion

        #region IDisposable Support
        bool LockedIsDisposed = false;

        void LockedThrowIfDisposed()
        {
            if (LockedIsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public void Dispose()
        {
            lock (LockObject)
            {
                if (LockedIsDisposed)
                {
                    return;
                }
                LockedIsDisposed = true;
            }
            Timer.Dispose();
        }
        #endregion
    }
}
