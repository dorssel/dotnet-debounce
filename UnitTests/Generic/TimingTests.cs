// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

namespace UnitTests.Generic;

/// <summary>
/// Most tests run a sequence at <see cref="TimingUnit"/> intervals.
/// <para>
/// Make sure that race conditions are avoided, or else tests may randomly fail.
/// For example, with a DebounceWindow of 2 (timing units), do not trigger 2 units after
/// the last trigger. Also, do not verify at the same timing unit step that a timer is supposed
/// to run out (which alters the state).
/// </para>
/// <para>
/// As a general rule, only have a single thing happen at each timing unit step. This includes the verification.
/// </para>
/// </summary>
[TestClass]
[TestCategory("Production")]
sealed class TimingGenericTests
{
    /// <summary>
    /// The maximum time slice of thread scheduling is 10 ms, both for Linux and for Windows.
    /// </summary>
    static readonly TimeSpan TimingUnitMarginOfError = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// A single timing unit should be short enough so tests run fast, and long enough
    /// so the task scheduler can cope.
    /// </summary>
    static readonly TimeSpan TimingUnit = 5 * TimingUnitMarginOfError;

    static void Sleep(double count)
    {
        Thread.Sleep(count * TimingUnit);
    }

    /// <summary>
    /// Run a sequence of actions. After each action we wait until the next <see cref="TimingUnit" /> interval.
    /// <para>
    /// NOTE: The wait also happens after the last action.
    /// </para>
    /// <para>
    /// As an example: a sequence of two actions runs the first action at T == 0, then waits until
    /// T == 1, then runs the second action at T == 1, then waits until T == 2, then returns. The total
    /// number of timing units consumed is therefore the same as the number of actions provided, with each
    /// action running at the start of the interval.
    /// </para>
    /// </summary>
    static async Task TimedSequence(IEnumerable<Action> actions)
    {
        var start = DateTime.UtcNow;
        var step = 0;
        foreach (var action in actions)
        {
            action.Invoke();
            ++step;
            // Correct for drift in long sequences.
            var waitFor = start + (step * TimingUnit) - DateTime.UtcNow;
            if (waitFor < TimingUnitMarginOfError)
            {
                // Too much drift.
                Assert.Fail("Timing outside margin of error; consider increasing the TimingUnit.");
            }
            await Task.Delay(waitFor);
        }
    }

    /// <summary>
    /// A helper action that does nothing (i.e., it just skips to the next timing unit interval).
    /// </summary>
    static void Skip() { }

    /// <summary>
    /// A helper <see cref="Action"/> that triggers the given debouncer with the given data.
    /// </summary>
    static Action Trigger(Debouncer<int> debouncer, int data)
    {
        return () => { debouncer.Trigger(data); };
    }

    #region Trigger
    [TestMethod]
    public async Task TriggersWithDataLimit()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = 2 * TimingUnit,
            DataLimit = 4
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow
            Trigger(debouncer, 1),
            // T == 1, the trigger resets the DebounceWindow
            Trigger(debouncer, 2),
            // T == 2, the trigger resets the DebounceWindow
            Trigger(debouncer, 3),
            // T == 3, the trigger resets the DebounceWindow, but count maximum has been reached => handler invoked
            Trigger(debouncer, 4),
            // T == 5, the trigger starts the DebounceWindow
            Trigger(debouncer, 5),
            // T == 6
            Skip,
            // T == 7, DebounceWindow runs out => handler invoked
            Skip,
            // T == 8
        ]);
        // Verify
        Assert.AreEqual(5L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        CollectionAssert.That.AreEqual([1, 2, 3, 4, 5], wrapper.TriggerData);
        CollectionAssert.That.AreEqual([5], wrapper.LastTriggerData);
    }

    [TestMethod]
    public async Task TriggersWithTimeoutAndDataLimitAndTimeoutWins()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = 3 * TimingUnit,
            DebounceTimeout = 5 * TimingUnit,
            DataLimit = 4
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow and DebounceTimeout
            Trigger(debouncer, 1),
            // T == 1
            Skip,
            // T == 2, the trigger resets the DebounceWindow
            Trigger(debouncer, 2),
            // T == 3
            Skip,
            // T == 4, the trigger resets the DebounceWindow
            Trigger(debouncer, 3),
            // T == 5, DebounceTimeout runs out
            Skip,
            // T == 6
            () => {
                Assert.AreEqual(1L, wrapper.HandlerCount);
                Assert.AreEqual(3L, wrapper.TriggerCount);
            },
            // T == 7, the trigger starts the DebounceWindow and DebounceTimeout
            Trigger(debouncer, 4),
            // T == 8
            Skip,
            // T == 9
            Skip,
            // T == 10, DebounceWindow runs out
            Skip,
            // T == 11
        ]);
        // Verify
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(4L, wrapper.TriggerCount);
        CollectionAssert.That.AreEqual([1, 2, 3, 4], wrapper.TriggerData);
        CollectionAssert.That.AreEqual([4], wrapper.LastTriggerData);
    }

    [TestMethod]
    public async Task TriggersWithTimeoutAndDataLimitAndDataLimitWins()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = 3 * TimingUnit,
            DebounceTimeout = 5 * TimingUnit,
            DataLimit = 2
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow and DebounceTimeout
            Trigger(debouncer, 1),
            // T == 1
            Skip,
            // T == 2, the trigger reaches DataLimit
            Trigger(debouncer, 2),
            // T == 3,
            () => {
                Assert.AreEqual(1L, wrapper.HandlerCount);
                Assert.AreEqual(2L, wrapper.TriggerCount);
            },
            // T == 4
            Skip,
            // T == 5, both DebounceWindow and DebounceTimeout would run out
            Skip,
            // T == 6
        ]);
        // Verify
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        CollectionAssert.That.AreEqual([1, 2], wrapper.TriggerData);
        CollectionAssert.That.AreEqual([1, 2], wrapper.LastTriggerData);
    }

    [TestMethod]
    public async Task LoweringDataLimitFiresEvent()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = 10 * TimingUnit,
            DataLimit = 3
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow
            Trigger(debouncer, 1),
            // T == 1
            Trigger(debouncer, 2),
            // T == 2, lowering DataLimit immediately fires event
            () => {
                Assert.AreEqual(0L, wrapper.HandlerCount);
                Assert.AreEqual(0L, wrapper.TriggerCount);
                CollectionAssert.That.AreEqual([], wrapper.TriggerData);
                CollectionAssert.That.AreEqual([], wrapper.LastTriggerData);
                debouncer.DataLimit = 1;
            },
            // T == 3
        ]);
        // Verify
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        CollectionAssert.That.AreEqual([1, 2], wrapper.TriggerData);
        CollectionAssert.That.AreEqual([1, 2], wrapper.LastTriggerData);
    }

    [TestMethod]
    public async Task TriggerBeyondDataLimitThrows()
    {
        using var debouncer = new Debouncer<int>
        {
            DataLimit = 1,
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        wrapper.Debounced += (s, e) => Sleep(3);
        await TimedSequence([
            // T == 0, the first trigger immediately causes a handler invocation
            Trigger(debouncer, 1),
            // T == 1, one third into the handler, the trigger gets buffered
            Trigger(debouncer, 2),
            // T == 2, two thirds into the handler, the trigger throws
            () => {
                _ = Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    debouncer.Trigger(3);
                });
            },
            // T == 3, the first handler returns, the seconds handler is immediately invoked
            Skip,
            // T == 4, one third into the handler
            Skip,
            // T == 5, two thirds into the handler
            Skip,
            // T == 6, second handler returns
            Skip,
            // T == 7
        ]);
        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        CollectionAssert.That.AreEqual([1, 2], wrapper.TriggerData);
        CollectionAssert.That.AreEqual([2], wrapper.LastTriggerData);
    }

    #endregion
}
