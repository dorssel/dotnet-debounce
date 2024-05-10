// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

namespace Dorssel.Utilities.Generic;

/// <summary>
/// Provides data for the <see cref="IDebouncerBase{TEventArgs}.Debounced"/> event.
/// </summary>
public class DebouncedEventArgs<TData> : DebouncedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebouncedEventArgs{TData}"/> class.
    /// </summary>
    /// <param name="count"></param>
    /// <param name="triggerData">
    /// Accumulated data from each individual trigger. Must not be empty."/>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="triggerData"/> is an empty list.</exception>
    public DebouncedEventArgs(long count, IReadOnlyList<TData> triggerData)
        : this(count, triggerData, true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebouncedEventArgs{TData}"/> class.
    /// </summary>
    /// <param name="count"></param>
    /// <param name="triggerData">
    /// Accumulated data from each individual trigger. Must not be empty if <paramref name="boundsCheck"/> is <c>true</c>.
    /// </param>
    /// <param name="boundsCheck">If <c>true</c>, <paramref name="triggerData"/> is checked to be non-empty.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="boundsCheck"/> is <see langword="true" /> and <paramref name="triggerData"/> is an empty list.</exception>
    protected DebouncedEventArgs(long count, IReadOnlyList<TData> triggerData, bool boundsCheck)
        : base(count, boundsCheck)
    {
        if (triggerData is null)
        {
            throw new ArgumentNullException(nameof(triggerData));
        }

        if (boundsCheck)
        {
            if (triggerData.Count > count)
            {
                throw new ArgumentOutOfRangeException(nameof(triggerData), $"{nameof(triggerData)} must not contain more than Count items");
            }
        }

        TriggerData = triggerData;
    }

    /// <summary>
    /// List of data accumulated in this buffered event.
    /// <para>
    /// Note that the trigger data is shared with all event listeners and that besides the list itself (which is read only),
    /// the data within the list should probably also not be altered.
    /// </para>
    /// </summary>
    public IReadOnlyList<TData> TriggerData { get; }
}
