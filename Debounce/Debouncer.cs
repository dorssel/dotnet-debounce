// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using Dorssel.Utilities.Generic;

namespace Dorssel.Utilities;

/// <summary>
/// Object which debounces events, i.e., accumulating multiple incoming events into one.
/// </summary>
public sealed class Debouncer : DebouncerBase<DebouncedEventArgs>, IDebouncer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer{TData}"/> class.
    /// </summary>
    public Debouncer()
        : base(TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer{TData}"/> class using the specified <see cref="TimeProvider"/>.
    /// </summary>
    /// <remarks>This constructor is intended for unit testing.</remarks>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to use.</param>
    public Debouncer(TimeProvider timeProvider)
        : base(timeProvider)
    {
    }

    private protected override DebouncedEventArgs LockedCreateEventArgs(long count)
    {
        return new(count);
    }

    private protected override void LockedReset() { }
}
