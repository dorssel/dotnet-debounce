// SPDX-FileCopyrightText: 2024 Alain van den Berg
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities;

/// <summary>
/// This interface specifies the public API for the <see cref="Bufferer{TEvent}"/> class.
/// </summary>
public interface IBufferer<TEvent> : IDebounceSettings
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

    /// <summary>Resets the accumulated events to an empty collection and cancels any ongoing buffering.</summary>
    /// <remarks>This method may be called even after <see cref="IDisposable.Dispose"/> has been called.</remarks>
    /// <returns>The number of events that had been accumulated since the last event handler was called.</returns>
    public long Reset();
}
