﻿// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities.Generic;

/// <summary>
/// Interface for debouncers accumulating triggers and debouncing.
/// </summary>
public interface IDebouncerBase<TEventArgs> where TEventArgs : DebouncedEventArgs
{
    /// <summary>
    /// This event will be sent when <see cref="Trigger"/> has been called one or more times and
    /// the debounce timer times out.
    /// </summary>
    public event EventHandler<TEventArgs> Debounced;

    /// <summary>Gets or sets the <see cref="TimeSpan"/> within which new calls to <see cref="Trigger"/> will restart the debounce timer.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan DebounceWindow { get; set; }

    /// <summary>Gets or sets the maximum <see cref="TimeSpan"/> since the first <see cref="Trigger"/> after which a new <see cref="Debounced"/> event will fire.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan DebounceTimeout { get; set; }

    /// <summary>Gets or sets the minimum <see cref="TimeSpan"/> between two consecutive starts of the <see cref="Debounced"/> event.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan EventSpacing { get; set; }

    /// <summary>Gets or sets the minimum <see cref="TimeSpan"/> between the end of one <see cref="Debounced"/> event handler invocation and the start of the next.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan HandlerSpacing { get; set; }

    /// <summary>Gets or sets the <see cref="TimeSpan"/> within which multiple calls to <see cref="Trigger"/> will be coalesced without rescheduling the timer.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan TimingGranularity { get; set; }

    /// <summary>Accumulates one more trigger.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public void Trigger();

    /// <summary>Resets the accumulated trigger count to 0 and cancels any ongoing debouncing.</summary>
    /// <remarks>This method may be called even after <see cref="IDisposable.Dispose"/> has been called.</remarks>
    /// <returns>The number of triggers that had been accumulated since the last event handler was called.</returns>
    public long Reset();

    /// <summary>Gets a waitable <see cref="Task"/> running any current event handlers.</summary>
    public Task CurrentEventHandlersTask { get; }
}
