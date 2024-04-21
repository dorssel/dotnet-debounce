// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using Dorssel.Utilities.Generic;

namespace Dorssel.Utilities;

/// <summary>
/// Object which debounces events, i.e., accumulating multiple incoming events into one.
/// </summary>
public sealed class Debouncer : Debouncer<Void>, IDebouncer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer"/> class.
    /// </summary>
    public Debouncer()
        : base(false)
    {
    }
}
