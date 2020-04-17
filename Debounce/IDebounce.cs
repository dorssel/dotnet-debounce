using System;

namespace Dorssel.Utility
{
    public class DebouncedEventArgs : EventArgs
    {
        internal DebouncedEventArgs()
        { }

        public DebouncedEventArgs(long count, DateTimeOffset firstTrigger, DateTimeOffset lastTrigger)
        {
            Count = count;
            FirstTrigger = firstTrigger;
            LastTrigger = lastTrigger;
        }

        public long Count { get; internal set; }
        public DateTimeOffset FirstTrigger { get; internal set; }
        public DateTimeOffset LastTrigger { get; internal set; }
    }

    interface IDebounce : IDisposable
    {
        event EventHandler<DebouncedEventArgs>? Debounced;

        void Trigger();

        public TimeSpan DebounceInterval { get; set; }
        public TimeSpan DebounceTimeout { get; set; }
        public TimeSpan BackoffInterval { get; set; }
    }

}
