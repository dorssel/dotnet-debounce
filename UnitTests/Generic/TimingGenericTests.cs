// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

using Microsoft.Extensions.Time.Testing;

namespace UnitTests.Generic;

[TestClass]
[TestCategory("Production")]
sealed class TimingGenericTests
{
    /// <summary>
    /// Arbitrary timespan, since we are using FakeTimeProvider.
    /// </summary>
    static readonly TimeSpan TimingUnit = TimeSpan.FromSeconds(1);

    #region Trigger
    [TestMethod]
    public async Task TriggersWithDataLimit()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer<int>(timeProvider)
        {
            DebounceWindow = 2 * TimingUnit,
            DataLimit = 4
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);

        // T == 0, the trigger starts the DebounceWindow
        debouncer.Trigger(1);
        timeProvider.Advance(TimingUnit);
        // T == 1, the trigger resets the DebounceWindow
        debouncer.Trigger(2);
        timeProvider.Advance(TimingUnit);
        // T == 2, the trigger resets the DebounceWindow
        debouncer.Trigger(3);
        timeProvider.Advance(TimingUnit);
        // T == 3, the trigger resets the DebounceWindow, but count maximum has been reached => handler invoked
        debouncer.Trigger(4);
        timeProvider.Advance(TimingUnit);
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);
        // T == 4, the trigger resets the DebounceWindow
        debouncer.Trigger(5);
        timeProvider.Advance(TimingUnit);
        // T == 5
        timeProvider.Advance(TimingUnit);
        // T == 6, DebounceWindow runs out => handler invoked
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(5L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        CollectionAssert.Instance.AreEqual([1, 2, 3, 4, 5], wrapper.TriggerData);
        CollectionAssert.Instance.AreEqual([5], wrapper.LastTriggerData);
    }

    [TestMethod]
    public async Task TriggersWithTimeoutAndDataLimitAndTimeoutWins()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer<int>(timeProvider)
        {
            DebounceWindow = 3 * TimingUnit,
            DebounceTimeout = 5 * TimingUnit,
            DataLimit = 4
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);

        // T == 0, the trigger starts the DebounceWindow and DebounceTimeout
        debouncer.Trigger(1);
        timeProvider.Advance(TimingUnit);
        // T == 1
        timeProvider.Advance(TimingUnit);
        // T == 2, the trigger resets the DebounceWindow
        debouncer.Trigger(2);
        timeProvider.Advance(TimingUnit);
        // T == 3
        timeProvider.Advance(TimingUnit);
        // T == 4, the trigger resets the DebounceWindow
        debouncer.Trigger(3);
        timeProvider.Advance(TimingUnit);
        // T == 5, DebounceTimeout runs out => handler invoked
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(3L, wrapper.TriggerCount);

        // the trigger starts the DebounceWindow and DebounceTimeout
        debouncer.Trigger(4);
        timeProvider.Advance(TimingUnit);
        // T == 6
        timeProvider.Advance(TimingUnit);
        // T == 7
        timeProvider.Advance(TimingUnit);
        // T == 8, DebounceWindow runs out => handler invoked
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(4L, wrapper.TriggerCount);
        CollectionAssert.Instance.AreEqual([1, 2, 3, 4], wrapper.TriggerData);
        CollectionAssert.Instance.AreEqual([4], wrapper.LastTriggerData);
    }

    [TestMethod]
    public async Task TriggersWithTimeoutAndDataLimitAndDataLimitWins()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer<int>(timeProvider)
        {
            DebounceWindow = 3 * TimingUnit,
            DebounceTimeout = 5 * TimingUnit,
            DataLimit = 2
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);

        // T == 0, the trigger starts the DebounceWindow and DebounceTimeout
        debouncer.Trigger(1);
        timeProvider.Advance(TimingUnit);
        // T == 1
        timeProvider.Advance(TimingUnit);
        // T == 2, the trigger reaches DataLimit => handler invoked
        debouncer.Trigger(2);
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(2L, wrapper.TriggerCount);

        timeProvider.Advance(TimingUnit);
        // T == 3
        timeProvider.Advance(TimingUnit);
        // T == 4
        timeProvider.Advance(TimingUnit);
        // T == 5, both DebounceWindow and DebounceTimeout would run out but nothing actually happens
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        CollectionAssert.Instance.AreEqual([1, 2], wrapper.TriggerData);
        CollectionAssert.Instance.AreEqual([1, 2], wrapper.LastTriggerData);
    }

    [TestMethod]
    public async Task LoweringDataLimitFiresEvent()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer<int>(timeProvider)
        {
            DebounceWindow = 10 * TimingUnit,
            DataLimit = 3
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);

        // T == 0, the trigger starts the DebounceWindow
        debouncer.Trigger(1);
        timeProvider.Advance(TimingUnit);
        // T == 1, the trigger resets the DebounceWindow
        debouncer.Trigger(2);
        timeProvider.Advance(TimingUnit);
        // T == 2, lowering DataLimit immediately fires event

        Assert.AreEqual(0L, wrapper.HandlerCount);
        Assert.AreEqual(0L, wrapper.TriggerCount);
        CollectionAssert.Instance.AreEqual([], wrapper.TriggerData);
        CollectionAssert.Instance.AreEqual([], wrapper.LastTriggerData);

        debouncer.DataLimit = 1;
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        CollectionAssert.Instance.AreEqual([1, 2], wrapper.TriggerData);
        CollectionAssert.Instance.AreEqual([1, 2], wrapper.LastTriggerData);
    }
    #endregion
}
