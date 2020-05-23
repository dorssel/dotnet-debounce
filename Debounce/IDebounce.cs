using System;

namespace Dorssel.Utility
{
    public delegate void DebouncedEventHandler(object sender, IDebouncedEventArgs e);

    public interface IDebounce : IDisposable
    {
        event DebouncedEventHandler Debounced;

        void Trigger();

        public TimeSpan DebounceWindow { get; set; }
        public TimeSpan DebounceTimeout { get; set; }
        public TimeSpan EventSpacing { get; set; }
        public TimeSpan HandlerSpacing { get; set; }
        public TimeSpan TimingGranularity { get; set; }
    }
}
