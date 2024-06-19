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
    /// <param name="count"><inheritdoc cref="DebouncedEventArgs(long)" path="/param[@name='count']"/>
    /// Both <see cref="IDebouncerBase{TEventArgs}.Trigger"/> and <see cref="IDebouncer{TData}.Trigger(TData)"/> add to the count.
    /// </param>
    /// <param name="triggerData">
    /// Accumulated data from each call to <see cref="IDebouncer{TData}.Trigger(TData)"/>."/>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><inheritdoc cref="DebouncedEventArgs(long)"/>
    /// Thrown when <paramref name="triggerData"/> has more than <paramref name="count"/> items.
    /// </exception>
    public DebouncedEventArgs(long count, IReadOnlyList<TData> triggerData)
        : this(count, triggerData, true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebouncedEventArgs{TData}"/> class.
    /// </summary>
    /// <param name="count"><inheritdoc cref="DebouncedEventArgs{TData}.DebouncedEventArgs(long, IReadOnlyList{TData})" path="/param[@name='count']"/></param>
    /// <param name="triggerData"><inheritdoc cref="DebouncedEventArgs{TData}.DebouncedEventArgs(long, IReadOnlyList{TData})" path="/param[@name='triggerData']"/></param>
    /// <param name="boundsCheck"><inheritdoc cref="DebouncedEventArgs(long, bool)" path="/param[@name='boundsCheck']"/></param>
    /// <exception cref="ArgumentOutOfRangeException"><inheritdoc cref="DebouncedEventArgs{TData}.DebouncedEventArgs(long, IReadOnlyList{TData})" path="/exception" />
    /// <para><inheritdoc cref="DebouncedEventArgs(long, bool)" path="/exception/para"/></para>
    /// </exception>
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
