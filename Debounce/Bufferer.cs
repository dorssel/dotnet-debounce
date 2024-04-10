// SPDX-FileCopyrightText: 2024 Alain van den Berg
//
// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;

namespace Dorssel.Utilities;

/// <summary>
/// A <see cref="Debouncer"/> that buffers data and sends events with the accumulated data.
/// </summary>
/// <typeparam name="TData">Data to buffer per trigger</typeparam>
/// <remarks>
/// This is not as performant as the <see cref="Debouncer"/> due to allocations.
/// </remarks>
public sealed class Bufferer<TData> : IDisposable, IBufferer<TData>
{
    IDebouncer debouncer;
    List<TData> eventList = new();
    object eventListLock = new();

    /// <summary>
    /// Create Bufferer with the default <see cref="Debouncer"/>.
    /// </summary>
#pragma warning disable CA2000 // Dispose objects before losing scope
    public Bufferer() : this(new Debouncer()) { }
#pragma warning restore CA2000 // Dispose objects before losing scope

    /// <summary>
    /// Create Bufferer with a specific <see cref="IDebouncer"/> instance.
    /// </summary>
    /// <param name="debouncer">The debouncer instance to use</param>
    /// <exception cref="ArgumentNullException">If debouncer is null</exception>
    public Bufferer(IDebouncer debouncer)
    {
        this.debouncer = debouncer ?? throw new ArgumentNullException(nameof(debouncer));
        debouncer.Debounced += Debouncer_Debounced;
    }

    /// <summary>
    /// Wrap the Debounced event and invoke the Buffered event instead.
    /// </summary>
    private void Debouncer_Debounced(object sender, DebouncedEventArgs e)
    {
        IReadOnlyList<TData> debouncedEvents;
        lock (eventListLock)
        {
            debouncedEvents = new ReadOnlyCollection<TData>(eventList);
            eventList = new();
        }

        Buffered?.Invoke(this, new BufferedEventArgs<TData>(debouncedEvents));
    }

    /// <inheritdoc/>
    public void Trigger(TData data)
    {
        lock (eventListLock)
        {
            eventList.Add(data);
        }
        debouncer.Trigger();
    }

    /// <inheritdoc/>
    public long Reset()
    {
        int count;
        lock (eventListLock)
        {
            count = eventList.Count;
            eventList = new();
        }
        debouncer.Reset();
        return count;
    }

    /// <inheritdoc/>
    public TimeSpan DebounceWindow
    {
        get => debouncer.DebounceWindow;
        set => debouncer.DebounceWindow = value;
    }

    /// <inheritdoc/>
    public TimeSpan DebounceTimeout
    {
        get => debouncer.DebounceTimeout;
        set => debouncer.DebounceTimeout = value;
    }

    /// <inheritdoc/>
    public TimeSpan EventSpacing
    {
        get => debouncer.EventSpacing;
        set => debouncer.EventSpacing = value;
    }

    /// <inheritdoc/>
    public TimeSpan HandlerSpacing
    {
        get => debouncer.HandlerSpacing;
        set => debouncer.HandlerSpacing = value;
    }

    /// <inheritdoc/>
    public TimeSpan TimingGranularity
    {
        get => debouncer.TimingGranularity;
        set => debouncer.TimingGranularity = value;
    }

    /// <inheritdoc/>
    public event EventHandler<BufferedEventArgs<TData>>? Buffered;

    /// <inheritdoc/>
    public void Dispose()
    {
        ((IDisposable)debouncer).Dispose();
    }
}
