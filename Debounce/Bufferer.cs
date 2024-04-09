// SPDX-FileCopyrightText: 2024 Alain van den Berg
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities;

/// <summary>
/// A <see cref="Debouncer"/> that buffers events of type <typeparamref name="TEvent"/> that were triggered, aggregates them and sends aggregated events.
/// </summary>
/// <typeparam name="TEvent">Events to buffer</typeparam>
/// <remarks>
/// This is not as performant as the <see cref="Debouncer"/> due to allocations.
/// </remarks>
public sealed class Bufferer<TEvent> : IDisposable, IBufferer<TEvent>
{
    IDebounce debouncer;
    List<TEvent> eventList = new();
    object eventListLock = new();

    /// <summary>
    /// Create Bufferer with the default <see cref="Debouncer"/>.
    /// </summary>
#pragma warning disable CA2000 // Dispose objects before losing scope
    public Bufferer() : this(new Debouncer()) { }
#pragma warning restore CA2000 // Dispose objects before losing scope

    /// <summary>
    /// Create Bufferer with a specific <see cref="IDebounce"/> instance.
    /// </summary>
    /// <param name="debouncer"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public Bufferer(IDebounce debouncer)
    {
        this.debouncer = debouncer ?? throw new ArgumentNullException(nameof(debouncer));
        debouncer.Debounced += Debouncer_Debounced;
    }

    /// <summary>
    /// Wrap the Debounced event and invoke the Buffered event instead.
    /// </summary>
    private void Debouncer_Debounced(object sender, DebouncedEventArgs e)
    {
        List<TEvent> debouncedEvents;
        lock (eventListLock)
        {
            debouncedEvents = eventList;
            eventList = new();
        }

        Buffered?.Invoke(this, new BufferedEventArgs<TEvent>(debouncedEvents));
    }

    /// <inheritdoc/>
    public void Trigger(TEvent evt)
    {
        lock (eventListLock)
        {
            eventList.Add(evt);
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
    public event EventHandler<BufferedEventArgs<TEvent>>? Buffered;

    /// <inheritdoc/>
    public void Dispose()
    {
        (debouncer as IDisposable)?.Dispose();
    }
}
