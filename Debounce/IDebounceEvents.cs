// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities;

/// <summary>
/// This interface specifies the public API for the <see cref="Debouncer"/> class.
/// </summary>
public interface IDebounceEvents : IDebounceSettings
{
    /// <summary>
    /// This event will be sent when <see cref="Trigger"/> has been called one or more times and
    /// the debounce timer times out.
    /// </summary>
    public event EventHandler<DebouncedEventArgs> Debounced;

    /// <summary>Accumulates one more trigger.</summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public void Trigger();

    /// <summary>Resets the accumulated trigger count to 0 and cancels any ongoing debouncing.</summary>
    /// <remarks>This method may be called even after <see cref="IDisposable.Dispose"/> has been called.</remarks>
    /// <returns>The number of triggers that had been accumulated since the last event handler was called.</returns>
    public long Reset();
}
