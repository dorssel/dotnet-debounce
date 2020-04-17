using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Dorssel.Utility
{
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
        readonly Timer Timer;
        bool SendingEvent = false;

        void OnTimer(object state)
        {
            var eventArgs = new DebouncedEventArgs();
            lock (LockObject)
            {
                Debug.Assert(!SendingEvent);
                if (SendingEvent || (EventArgs.Count == 0))
                {
                    return;
                }
                var temp = EventArgs;
                EventArgs = eventArgs;
                eventArgs = temp;
                SendingEvent = true;
            }
            Debounced?.Invoke(this, eventArgs);
            lock (LockObject)
            {
                SendingEvent = false;
                LockedReschedule();
            }
        }

        #region IDebounce Support
        public event EventHandler<DebouncedEventArgs>? Debounced;

        public void Trigger()
        {
            lock (LockObject)
            {
                LockedThrowIfDisposed();
                var now = DateTimeOffset.Now;
                if (EventArgs.Count++ == 0)
                {
                    EventArgs.FirstTrigger = now;
                    FirstTriggerStopwatch.Start();
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
                // nothing to schedule
                return;
            }
            Timer.Change(_LockedDebounceInterval, Timeout.InfiniteTimeSpan);
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
