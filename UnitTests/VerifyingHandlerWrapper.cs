using System;
using System.Threading;
using Dorssel.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    sealed class VerifyingHandlerWrapper : IDisposable
    {
        public VerifyingHandlerWrapper(IDebounce debouncer)
        {
            Debouncer = debouncer;
            Debouncer.Debounced += OnDebounced;
        }

        public event EventHandler<IDebouncedEventArgs>? Debounced;

        public ulong HandlerCount { get; private set; }
        public ulong TriggerCount { get; private set; }

        void OnDebounced(object? sender, IDebouncedEventArgs debouncedEventArgs)
        {
            // sender *must* be the original debouncer object
            Assert.AreSame(Debouncer, sender);
            // *must* have a positive trigger count since last handler called
            Assert.IsTrue(debouncedEventArgs.Count > 0);
            // *never* should be called reentrant (i.e. always serialize handlers)
            Assert.AreEqual(Interlocked.Increment(ref ReentrancyCount), 1);

            ++HandlerCount;
            TriggerCount += debouncedEventArgs.Count;

            Debounced?.Invoke(this, debouncedEventArgs);

            // *never* should be called reentrant (i.e. always serialize handlers)
            Assert.AreEqual(Interlocked.Decrement(ref ReentrancyCount), 0);
        }

        readonly IDebounce Debouncer;
        int ReentrancyCount = 0;

        #region IDisposable Support
        int IsDisposed = 0;

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref IsDisposed, 1, 0) == 0)
            {
                Debouncer.Debounced -= OnDebounced;
            }
        }
        #endregion
    }
}
