// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities
{
    /// <summary>
    /// This interface specifies the event details passed to <see cref="DebouncedEventHandler"/>.
    /// </summary>
    public interface IDebouncedEventArgs
    {
        /// <summary>
        /// The number of triggers accumulated since the previous event was sent.
        /// </summary>
        /// <remarks>
        /// <para>Consumers of this interface may assume that the value is always greater than 0.</para>
        /// <para>Implementations of this interface must ensure that the value is always greater than 0.</para>
        /// </remarks>
        public long Count { get; }
    }
}
