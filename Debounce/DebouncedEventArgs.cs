// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using Dorssel.Utilities.Generic;

namespace Dorssel.Utilities;

/// <summary>
/// Provides data for the <see cref="IDebouncerBase{TEventArgs}.Debounced"/> event.
/// </summary>
public class DebouncedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebouncedEventArgs"/> class.
    /// </summary>
    /// <param name="count">The number of triggers accumulated since the previous event was sent.
    /// <para>Must be greater than 0.</para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is not greater than 0.</exception>
    public DebouncedEventArgs(long count)
        : this(count, true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebouncedEventArgs"/> class.
    /// </summary>
    /// <param name="count">The number of triggers accumulated since the previous event was sent.
    /// <para>Must be greater than 0 if <paramref name="boundsCheck"/> is <c>true</c>.</para>
    /// </param>
    /// <param name="boundsCheck">If <c>true</c>, <paramref name="count"/> is checked to be within its valid range.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="boundsCheck"/> is <c>true</c> and <paramref name="count"/> is not greater than 0.</exception>
    protected DebouncedEventArgs(long count, bool boundsCheck)
    {
        if (boundsCheck)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} must be greater than 0");
            }
        }

        Count = count;
    }

    /// <summary>
    /// The number of triggers accumulated since the previous event was sent.
    /// </summary>
    /// <remarks>
    /// <para>The value will always greater than 0.</para>
    /// </remarks>
    public long Count { get; }
}
