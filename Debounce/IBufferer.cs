// SPDX-FileCopyrightText: 2024 Alain van den Berg
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities;

/// <summary>
/// This interface specifies the public API for the <see cref="Bufferer{TData}"/> class.
/// </summary>
public interface IBufferer<TData> : IDebounceSettings
{
    /// <summary>
    /// This event will be sent when <see cref="Trigger"/> has been called one or more times and
    /// the debounce timer times out and will contain all events that have been triggered.
    /// </summary>
    public event EventHandler<BufferedEventArgs<TData>> Buffered;

    /// <summary>Accumulates the data of one more trigger.</summary>
    /// <param name="data">Data to be buffered.</param>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public void Trigger(TData data);

    /// <summary>Resets the accumulated data to an empty collection and cancels any ongoing buffering.</summary>
    /// <remarks>This method may be called even after <see cref="IDisposable.Dispose"/> has been called.</remarks>
    /// <returns>The number of data that had been accumulated since the last event handler was called.</returns>
    public long Reset();
}
