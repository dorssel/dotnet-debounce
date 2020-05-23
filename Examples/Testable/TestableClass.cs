using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Dorssel.Utility;

[assembly: InternalsVisibleTo("TestableUnitTests")]

namespace Testable
{
    public sealed class TestableClass : IDisposable
    {
        public TestableClass(IDebounce debounce)
        {
            Debounce = debounce ?? throw new ArgumentNullException(nameof(debounce));
            Debounce.Debounced += OnDebouncedEvents;
        }

        void OnDebouncedEvents(object sender, IDebouncedEventArgs debouncedEventArgs)
        {
            if (sender == null)
            {
                // Real Debouncer will never call with null arguments, but we can mock it!
                return;
            }
            if (debouncedEventArgs == null)
            {
                // Real Debouncer will never call with null arguments, but we can mock it!
                return;
            }

            if (debouncedEventArgs.Count == 0)
            {
                // Real DebouncedEventArgs will never contain Count == 0, but we can mock it!
                Debug.WriteLine("Safety first: we even covered this impossible case.");
            }
            else if (debouncedEventArgs.Count == ulong.MaxValue)
            {
                // It would take ages to reach this Count value for real DebouncedEventArgs, but we can mock it! 
                Debug.WriteLine("Corner case galore.");
            }
            else
            {
                Debug.WriteLine("Happy flow.");
            }
        }

        readonly IDebounce Debounce;

        #region IDisposable Support
        bool IsDisposed = false;

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Debounce.Debounced -= OnDebouncedEvents;
                Debounce.Dispose();
                IsDisposed = true;
            }
        }
        #endregion
    }
}
