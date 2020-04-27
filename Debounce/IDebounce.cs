using System;

namespace Dorssel.Utility
{
    public interface IDebouncedEventArgs
    {
        public long Count { get; }
    }

    public interface IDebounce : IDisposable
    {
        event EventHandler<IDebouncedEventArgs>? Debounced;

        void Trigger();

        public TimeSpan DebounceInterval { get; set; }
        public TimeSpan DebounceTimeout { get; set; }
        public TimeSpan BackoffInterval { get; set; }
    }
}
