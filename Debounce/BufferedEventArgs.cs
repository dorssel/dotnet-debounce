// SPDX-FileCopyrightText: 2024 Alain van den Berg
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities;

/// <summary>
/// Event arguments for the <see cref="Bufferer{TData}"/> event.
/// </summary>
public class BufferedEventArgs<TData> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BufferedEventArgs{TData}"/> class.
    /// </summary>
    /// <param name="bufferedData">
    /// The original data accumulated since the previous buffered event was sent.
    /// </param>
    public BufferedEventArgs(IReadOnlyList<TData> bufferedData)
    {
        Buffer = bufferedData;
    }

    /// <summary>
    /// List of data accumulated in this buffered event.
    /// </summary>
    public IReadOnlyList<TData> Buffer { get; }
}
