// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using Dorssel.Utilities;
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

        public event DebouncedEventHandler? Debounced;

        public ulong HandlerCount { get; private set; }
        public ulong TriggerCount { get; private set; }

        void OnDebounced(object sender, IDebouncedEventArgs debouncedEventArgs)
        {
            // sender *must* be the original debouncer object
            Assert.AreSame(Debouncer, sender);
            // *must* have a positive trigger count since last handler called
            Assert.IsTrue(debouncedEventArgs.Count > 0);
            // *never* should be called reentrant (i.e. always serialize handlers)
            Assert.AreEqual(Interlocked.Increment(ref ReentrancyCount), 1);

            ++HandlerCount;
            TriggerCount += (ulong)debouncedEventArgs.Count;

            Debounced?.Invoke(this, debouncedEventArgs);

            // *never* should be called reentrant (i.e. always serialize handlers)
            Assert.AreEqual(Interlocked.Decrement(ref ReentrancyCount), 0);
        }

        readonly IDebounce Debouncer;
        int ReentrancyCount;

        #region IDisposable Support
        int IsDisposed;

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
