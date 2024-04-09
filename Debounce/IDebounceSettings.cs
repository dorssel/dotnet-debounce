// SPDX-FileCopyrightText: 2024 Alain van den Berg
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities;

/// <summary>
/// Debounce settings for <see cref="Debouncer"/> and <see cref="Bufferer{TEvent}"/>.
/// </summary>
public interface IDebounceSettings
{
    /// <summary>Gets or sets the <see cref="TimeSpan"/> within which new calls to .Trigger() will restart the debounce timer.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan DebounceWindow { get; set; }

    /// <summary>Gets or sets the <see cref="TimeSpan"/> since the first .Trigger() after the which a new Debounced event will fire.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan DebounceTimeout { get; set; }

    /// <summary>Gets or sets the minimum <see cref="TimeSpan"/> between two consecutive starts of the Debounced event.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan EventSpacing { get; set; }

    /// <summary>Gets or sets the minimum <see cref="TimeSpan"/> between the end of one Debounced event handler invocation and the start of the next.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan HandlerSpacing { get; set; }

    /// <summary>Gets or sets the <see cref="TimeSpan"/> within which multiple calls to .Trigger()  will be coalesced without rescheduling the timer.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan TimingGranularity { get; set; }
}
