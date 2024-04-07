// SPDX-FileCopyrightText: 2024 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

namespace Dorssel.Utilities;

/// <summary>
/// This interface specifies the public API for the <see cref="Bufferer{TEvent}"/> class.
/// </summary>
public interface IBufferer<TEvent>
{
    /// <summary>
    /// This event will be sent when <see cref="Trigger"/> has been called one or more times and
    /// the debounce timer times out and will contain all events that have been triggered.
    /// </summary>
    public event EventHandler<BufferedEventArgs<TEvent>> Buffered;

    /// <summary>
    /// Trigger an event of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <param name="evt">Event to trigger.</param>
    public void Trigger(TEvent evt);

    /// <summary>Resets the accumulated trigger count to 0 and cancels any ongoing debouncing.</summary>
    /// <remarks>This method may be called even after <see cref="IDisposable.Dispose"/> has been called.</remarks>
    /// <returns>The number of events that had been accumulated since the last event handler was called.</returns>
    public long Reset();

    /// <summary>Gets or sets the <see cref="TimeSpan"/> within which new calls to <see cref="Trigger"/> will restart the debounce timer.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan DebounceWindow { get; set; }

    /// <summary>Gets or sets the <see cref="TimeSpan"/> since the first <see cref="Trigger"/> after the which a new <see cref="Buffered"/> event will fire.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan DebounceTimeout { get; set; }

    /// <summary>Gets or sets the minimum <see cref="TimeSpan"/> between two consecutive starts of the <see cref="Buffered"/> event.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan EventSpacing { get; set; }

    /// <summary>Gets or sets the minimum <see cref="TimeSpan"/> between the end of one <see cref="Buffered"/> event handler invocation and the start of the next.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan HandlerSpacing { get; set; }

    /// <summary>Gets or sets the <see cref="TimeSpan"/> within which multiple calls to <see cref="Trigger"/> will be coalesced without rescheduling the timer.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public TimeSpan TimingGranularity { get; set; }
}
