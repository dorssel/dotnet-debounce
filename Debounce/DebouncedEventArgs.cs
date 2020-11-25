using System;

namespace Dorssel.Utility
{
    sealed class DebouncedEventArgs : EventArgs, IDebouncedEventArgs
    {
        public DebouncedEventArgs(long count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} must be greater than 0");
            }
            Count = count;
        }

        public long Count { get; }
    }
}
