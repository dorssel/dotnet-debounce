// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities;

/// <summary>
/// Void data type for <see cref="IDebouncer"/>.
/// </summary>
public struct Void { }

/// <summary>
/// This interface specifies the public API for the <see cref="Debouncer"/> class.
/// </summary>
public interface IDebouncer : IDebouncer<Void, DebouncedEventArgs>
{
}

/// <summary>
/// Interface for debouncers accumulating triggers and debouncing.
/// </summary>
/// <typeparam name="TData">Data to accumulate when triggering</typeparam>
/// <typeparam name="TEventArgs">Type of event arguments for the <see cref="Debounced"/> event</typeparam>
public interface IDebouncer<TData, TEventArgs> : IDebounceSettings
    where TEventArgs : EventArgs
{
    /// <summary>
    /// This event will be sent when <see cref="Trigger"/> has been called one or more times and
    /// the debounce timer times out.
    /// </summary>
    public event EventHandler<TEventArgs> Debounced;

    /// <summary>Accumulates one more trigger.</summary>
    /// <param name="data">Data that accompanies the trigger, or null if no data.</param>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    public void Trigger(TData? data = default);

    /// <summary>Resets the accumulated trigger count to 0 and cancels any ongoing debouncing.</summary>
    /// <remarks>This method may be called even after <see cref="IDisposable.Dispose"/> has been called.</remarks>
    /// <returns>The number of triggers that had been accumulated since the last event handler was called.</returns>
    public long Reset();
}

