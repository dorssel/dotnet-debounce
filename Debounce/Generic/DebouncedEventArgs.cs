// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

namespace Dorssel.Utilities.Generic;

/// <summary>
/// Provides data for the <see cref="Debouncer{TData}.Debounced"/> event.
/// </summary>
public class DebouncedEventArgs<TData> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebouncedEventArgs{TData}"/> class.
    /// </summary>
    /// <param name="count">The number of triggers accumulated since the previous event was sent.
    /// <para>Must be greater than 0.</para>
    /// </param>
    /// <param name="triggerData">
    /// Accumulated data from each individual trigger, or empty when buffering is disabled in the <see cref="Debouncer{TData}"/>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is not greater than 0.</exception>
    public DebouncedEventArgs(long count, IReadOnlyList<TData> triggerData)
        : this(count, true, triggerData)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebouncedEventArgs{TData}"/> class.
    /// </summary>
    /// <param name="count">The number of triggers accumulated since the previous event was sent.
    /// <para>Must be greater than 0 if <paramref name="boundsCheck"/> is <c>true</c>.</para>
    /// </param>
    /// <param name="boundsCheck">If <c>true</c>, <paramref name="count"/> is checked to be within its valid range.</param>
    /// <param name="triggerData">
    /// Accumulated data from each individual trigger, or empty when buffering is disabled in the <see cref="Debouncer{TData}"/>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="boundsCheck"/> is <c>true</c> and <paramref name="count"/> is not greater than 0.</exception>
    protected DebouncedEventArgs(long count, bool boundsCheck, IReadOnlyList<TData> triggerData)
    {
        if (boundsCheck)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} must be greater than 0");
            }
        }

        Count = count;
        TriggerData = triggerData;
    }

    /// <summary>
    /// The number of triggers accumulated since the previous event was sent.
    /// </summary>
    /// <remarks>
    /// <para>The value will always greater than 0.</para>
    /// </remarks>
    public long Count { get; }

    /// <summary>
    /// List of data accumulated in this buffered event.
    /// </summary>
    public IReadOnlyList<TData> TriggerData { get; }
}
