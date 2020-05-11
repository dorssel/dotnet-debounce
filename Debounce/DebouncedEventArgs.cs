using System;

namespace Dorssel.Utility
{
    public sealed class DebouncedEventArgs : EventArgs, IDebouncedEventArgs
    {
        internal DebouncedEventArgs(ulong count)
        {
            if (count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} must be greater than 0");
            }
            Count = count;
        }

        public ulong Count { get; }
    }
}
