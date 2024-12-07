// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

using Microsoft.Extensions.Time.Testing;

namespace UnitTests;

[TestClass]
[TestCategory("Production")]
sealed class TimingTests
{
    /// <summary>
    /// Arbitrary timespan, since we are using FakeTimeProvider.
    /// </summary>
    static readonly TimeSpan TimingUnit = TimeSpan.FromSeconds(1);

    #region Dispose
    [TestMethod]
    public async Task DisposeDuringTimer()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider)
        {
            DebounceWindow = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        // T == 0, the trigger starts the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 1, Dispose in the middle of the DebounceWindow
        debouncer.Dispose();
        timeProvider.Advance(TimingUnit);
        // T == 2, DebounceWindow would run out, but the object is disposed already
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify that the handler was indeed *not* called, even though there was a trigger and the debounce window ran out.
        Assert.AreEqual(0L, wrapper.HandlerCount);
        _ = Assert.ThrowsException<ObjectDisposedException>(debouncer.Trigger);
    }
    #endregion

    #region Trigger
    [TestMethod]
    public async Task TriggerSingleDelay()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider)
        {
            DebounceWindow = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        // T == 0, the trigger starts the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 1, in the middle of the DebounceWindow

        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);

        timeProvider.Advance(TimingUnit);
        // T == 2, DebounceWindow runs out
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggersWithTimeout()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider)
        {
            DebounceWindow = 3 * TimingUnit,
            DebounceTimeout = 5 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        // T == 0, the trigger starts the DebounceWindow and the DebounceTimeout
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 1, the trigger resets the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 2, the trigger resets the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 3, the trigger resets the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 4, the trigger resets the DebounceWindow
        debouncer.Trigger();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);

        timeProvider.Advance(TimingUnit);
        // T == 5, DebounceTimeout runs out => handler invoked
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(5L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);

        // the trigger starts the DebounceWindow and the DebounceTimeout
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 7
        timeProvider.Advance(TimingUnit);
        // T == 8
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(5L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);

        timeProvider.Advance(TimingUnit);
        // T == 9, DebounceWindow runs out -> handler invoked
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(6L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggerCoalescence()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider)
        {
            DebounceWindow = 3 * TimingUnit,
            TimingGranularity = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        // T == 0, the first trigger starts the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 1, the trigger is coalesced, the DebounceWindow is not reset
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 2, the timing granularity is hit and the DebounceWindow is reset (as coalescence took place)
        timeProvider.Advance(TimingUnit);
        // T == 3
        timeProvider.Advance(TimingUnit);
        // T == 4, the DebounceWindow would have run out, but it was reset earlier due to timing granularity + coalescence
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);

        timeProvider.Advance(TimingUnit);
        // T == 5, DebounceWindow runs out
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggerDuringHandlerSpacing()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider)
        {
            HandlerSpacing = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        // T == 0, the trigger immediately causes a handler invocation
        debouncer.Trigger();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);

        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 1, verify that the seconds handler is not called yet
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);

        timeProvider.Advance(TimingUnit);
        // T == 2, HandlerSpacing runs out, second handler invoked
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggerDuringEventSpacing()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider)
        {
            EventSpacing = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        // T == 0, the trigger immediately causes a handler invocation
        debouncer.Trigger();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);

        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 1, verify that the seconds handler is not called yet
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);

        timeProvider.Advance(TimingUnit);
        // T == 2, EventSpacing runs out, second handler invoked
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task CoalesceDuringHandler()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider)
        {
            DebounceWindow = 3 * TimingUnit,
            TimingGranularity = 2 * TimingUnit,
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        using var started = new SemaphoreSlim(0);
        using var finish = new SemaphoreSlim(0);
        wrapper.Debounced += (s, e) =>
        {
            _ = started.Release();
            finish.Wait();
        };

        // T == 0, the trigger starts the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 1
        timeProvider.Advance(TimingUnit);
        // T == 2, TimingGranularity runs out, resets DebounceWindow
        timeProvider.Advance(TimingUnit);
        // T == 3, DebounceWindow would run out, but was reset due to TimingGranularity + coalescence
        timeProvider.Advance(TimingUnit);
        // T == 4
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);

        timeProvider.Advance(TimingUnit);
        // T == 5, DebounceWindow runs out, first handler starts
        await started.WaitAsync();

        // the trigger starts the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 6, the trigger coalesces and does not reset the DebounceWindow
        debouncer.Trigger();
        // make the handler return, which reschedules the timer, which restarts the TimingGranularity
        _ = finish.Release();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);
        timeProvider.Advance(TimingUnit);
        // T == 7
        timeProvider.Advance(TimingUnit);
        // T == 8, TimingGranularity runs out and resets the DebounceWindow
        timeProvider.Advance(TimingUnit);
        // T == 9, second DebounceWindow would run out, but was reset due to TimingGranularity + coalescence
        timeProvider.Advance(TimingUnit);
        // T == 10
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);

        timeProvider.Advance(TimingUnit);
        // T == 11, DebounceWindow runs out, second handler starts
        await started.WaitAsync();
        _ = finish.Release();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(3L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }
    #endregion

    #region Reset
    [TestMethod]
    public async Task ResetDuringDebounce()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider)
        {
            DebounceWindow = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        // T == 0, the trigger starts the DebounceWindow
        debouncer.Trigger();
        timeProvider.Advance(TimingUnit);
        // T == 1, Reset during debounce

        Assert.AreEqual(1L, debouncer.Reset());

        timeProvider.Advance(TimingUnit);
        // T == 2, DebounceWindow would run out, but current count is 0
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);
    }
    #endregion
}
