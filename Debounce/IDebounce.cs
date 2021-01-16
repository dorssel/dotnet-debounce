// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System;

namespace Dorssel.Utility
{
    public delegate void DebouncedEventHandler(object sender, IDebouncedEventArgs e);

    public interface IDebounce : IDisposable
    {
        event DebouncedEventHandler Debounced;

        /// <summary>Accumulates one more trigger.</summary>
        /// <exception cref="ObjectDisposedException" />
        void Trigger();

        /// <summary>Resets the accumulated trigger count to 0 and cancels any ongoing debouncing.</summary>
        /// <remarks>This method may be called even after <see cref="IDisposable.Dispose"/> has been called.</remarks>
        /// <returns>The number of triggers that had been accumulated since the last event handler was called.</returns>
        long Reset();

        /// <summary>Gets or sets the <see cref="TimeSpan"/> within which new calls to <see cref="Trigger"/> will restart the debounce timer.</summary>
        /// <returns>The number of triggers that had been accumulated since the last event handler was called.</returns>
        /// <exception cref="ObjectDisposedException" />
        TimeSpan DebounceWindow { get; set; }

        /// <summary>Gets or sets the <see cref="TimeSpan"/> since the first <see cref="Trigger"/> after the which a new <see cref="Debounced"/> event will fire.</summary>
        /// <exception cref="ObjectDisposedException" />
        TimeSpan DebounceTimeout { get; set; }

        /// <summary>Gets or sets the minimum <see cref="TimeSpan"/> between two consecutive starts of the <see cref="Debounced"/> event.</summary>
        /// <exception cref="ObjectDisposedException" />
        TimeSpan EventSpacing { get; set; }

        /// <summary>Gets or sets the minimum <see cref="TimeSpan"/> between the end of one <see cref="Debounced"/> event handler invocation and the start of the next.</summary>
        /// <exception cref="ObjectDisposedException" />
        TimeSpan HandlerSpacing { get; set; }

        /// <summary>Gets or sets the <see cref="TimeSpan"/> within which multiple calls to <see cref="Trigger"/> will be coalesced without rescheduling the timer.</summary>
        /// <exception cref="ObjectDisposedException" />
        TimeSpan TimingGranularity { get; set; }
    }
}
