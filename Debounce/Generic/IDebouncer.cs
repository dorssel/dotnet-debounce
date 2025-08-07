// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

namespace Dorssel.Utilities.Generic;

/// <summary>
/// Interface for debouncers accumulating triggers and debouncing.
/// </summary>
/// <typeparam name="TData">Data to accumulate when triggering</typeparam>
public interface IDebouncer<TData> : IDebouncerBase<DebouncedEventArgs<TData>>
{
    /// <summary>
    /// Gets or sets the maximum number of calls to <see cref="Trigger(TData)"/> after which
    /// a new <see cref="IDebouncerBase{TEventArgs}.Debounced"/> event will fire.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    int DataLimit { get; set; }

    /// <summary>Accumulates one more trigger.</summary>
    /// <param name="data">Data that accompanies the trigger.</param>
    /// <exception cref="InvalidOperationException">
    /// More than <see cref="DataLimit"/> calls to <see cref="Trigger(TData)"/> while an event handler is currently being invoked.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
    void Trigger(TData data);

    /// <inheritdoc cref="IDebouncerBase{TEventArgs}.Reset"/>
    /// <param name="data">Accumulated data from each call to <see cref="IDebouncer{TData}.Trigger(TData)"/> since the last event handler was called.</param>
    long Reset(out IReadOnlyList<TData> data);
}
