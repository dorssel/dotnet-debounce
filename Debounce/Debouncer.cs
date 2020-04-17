using System;
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
            Timer.Change(LockedMinimumDebounceTime, Timeout.InfiniteTimeSpan);
        }

        TimeSpan LockedMinimumDebounceTime = TimeSpan.Zero;
        public TimeSpan MinimumDebounceTime {
            get
            {
                lock (LockObject)
                {
                    return LockedMinimumDebounceTime;
                }
            }
            set
            {
                if (value.CompareTo(TimeSpan.Zero) < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MinimumDebounceTime));
                }
                lock (LockObject)
                {
                    LockedMinimumDebounceTime = value;
                    LockedReschedule();
                }
            }
        }

        readonly TimeSpan _MaximumDebounceTime = Timeout.InfiniteTimeSpan;
        public TimeSpan MaximumDebounceTime {
            get => _MaximumDebounceTime;
            set => throw new NotImplementedException();
        }

        readonly TimeSpan _BackoffTime = TimeSpan.Zero;
        public TimeSpan BackoffTime {
            get => _BackoffTime;
            set => throw new NotImplementedException();
        }
        #endregion

        #region IDisposable Support
        bool IsDisposed = false;

        void LockedThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Timer.Dispose();
                }
                IsDisposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
