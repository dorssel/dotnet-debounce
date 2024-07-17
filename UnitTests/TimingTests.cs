// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

namespace UnitTests;

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
public class TimingTests
{
    /// <summary>
    /// The maximum time slice of thread scheduling is 10 ms, both for Linux and for Windows.
    /// Using twice that value should normally work, unless the CPU is really busy.
    /// </summary>
    static readonly TimeSpan TimingUnitMarginOfError = TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// A single timing unit should be short enough so tests run fast, and long enough
    /// so the task scheduler can cope.
    /// </summary>
    static readonly TimeSpan TimingUnit = 5 * TimingUnitMarginOfError;

    static void Sleep(double count) => Thread.Sleep(count * TimingUnit);

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
            var waitFor = start + step * TimingUnit - DateTime.UtcNow;
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

    #region Dispose
    [TestMethod]
    public async Task DisposeDuringTimer()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow
            debouncer.Trigger,
            // T == 1, Dispose in the middle of the DebounceWindow
            debouncer.Dispose,
            // T == 2, DebounceWindow would run out, but the object is disposed already
            Skip,
            // T == 3
        ]);
        // Verify that the handler was indeed *not* called, even though there was a trigger and the debounce window ran out.
        Assert.AreEqual(0L, wrapper.HandlerCount);
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.Trigger());
    }

    /// <summary>
    /// Verify that Dispose() can be called safely while a handler is running.
    /// </summary>
    [TestMethod]
    public async Task DisposeDuringHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        using var started = new ManualResetEventSlim();
        using var done = new ManualResetEventSlim();
        wrapper.Debounced += (s, e) =>
        {
            started.Set();
            Sleep(2);
            done.Set();
        };
        await TimedSequence([
            // T == 0, the trigger immediately causes a handler invocation
            debouncer.Trigger,
            // T == 1, in the middle of the handler Sleep
            () => {
                Assert.IsTrue(started.IsSet);
                Assert.IsFalse(done.IsSet);
                debouncer.Dispose();
            },
            // T == 2, handler exits
            Skip,
            // T == 3
        ]);
        // Verify
        Assert.IsTrue(done.IsSet);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.Trigger());
    }

    /// <summary>
    /// Verify that Dispose() can be called safely from within a handler.
    /// </summary>
    [TestMethod]
    public async Task DisposeFromHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        using var done = new ManualResetEventSlim();
        wrapper.Debounced += (s, e) =>
        {
            debouncer.Dispose();
            done.Set();
        };
        await TimedSequence([
            // T == 0, the trigger immediately causes a handler invocation
            debouncer.Trigger,
            // T == 1
        ]);
        // Verify
        Assert.IsTrue(done.IsSet);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.Trigger());
    }
    #endregion

    #region Trigger
    [TestMethod]
    public async Task TriggerSingle()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, the trigger immediately causes a handler invocation
            debouncer.Trigger,
            // T == 1
        ]);
        // Verify
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggerSingleDelay()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow
            debouncer.Trigger,
            // T == 1, in the middle of the DebounceWindow
            () => {
                Assert.AreEqual(0L, wrapper.TriggerCount);
                Assert.AreEqual(0L, wrapper.HandlerCount);
            },
            // T == 2, DebounceWindow runs out
            Skip,
            // T == 3
        ]);
        // Verify
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggersWithTimeout()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = 3 * TimingUnit,
            DebounceTimeout = 5 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow and the DebounceTimeout
            debouncer.Trigger,
            // T == 1, the trigger resets the DebounceWindow
            debouncer.Trigger,
            // T == 2, the trigger resets the DebounceWindow
            debouncer.Trigger,
            // T == 3, the trigger resets the DebounceWindow
            debouncer.Trigger,
            // T == 4, the trigger resets the DebounceWindow
            debouncer.Trigger,
            // T == 5, DebounceTimeout runs out
            Skip,
            // T == 6, Verify
            () => {
                Assert.AreEqual(5L, wrapper.TriggerCount);
                Assert.AreEqual(1L, wrapper.HandlerCount);
            },
            // T == 7, the trigger starts the DebounceWindow and the DebounceTimeout
            debouncer.Trigger,
            // T == 8
            Skip,
            // T == 9
            Skip,
            // T == 10, DebounceWindow runs out
            Skip,
            // T == 11
        ]);
        // Verify
        Assert.AreEqual(6L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggerCoalescence()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = 1 * TimingUnit,
            TimingGranularity = 0.1 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, the first trigger starts the DebounceWindow, the second is coalesced
            () => {
                debouncer.Trigger();
                debouncer.Trigger();
            },
            // T == 1, DebounceWindow runs out
            Skip,
            // T == 2
        ]);
        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggerFromHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        var first = true;

        wrapper.Debounced += (s, e) =>
        {
            if (first)
            {
                debouncer.Trigger();
                first = false;
            }
        };
        await TimedSequence([
            // T == 0, the trigger immediately causes a handler invocation
            debouncer.Trigger,
            // T == 1
        ]);
        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggerDuringHandlerSpacing()
    {
        using var debouncer = new Debouncer()
        {
            HandlerSpacing = 3 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, the trigger immediately causes a handler invocation
            debouncer.Trigger,
            // T == 1, verify first handler, add another trigger
            () => {
                Assert.AreEqual(1L, wrapper.TriggerCount);
                Assert.AreEqual(1L, wrapper.HandlerCount);
                debouncer.Trigger();
            },
            // T == 2, verify that the seconds handler is not called yet
            () => {
                Assert.AreEqual(1L, wrapper.TriggerCount);
                Assert.AreEqual(1L, wrapper.HandlerCount);
            },
            // T == 3, HandlerSpacing runs out, second handler invoked
            Skip,
            // T == 4
        ]);
        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task TriggerDuringEventSpacing()
    {
        using var debouncer = new Debouncer()
        {
            EventSpacing = 3 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, the trigger immediately causes a handler invocation
            debouncer.Trigger,
            // T == 1, verify first handler, add another trigger
            () => {
                Assert.AreEqual(1L, wrapper.TriggerCount);
                Assert.AreEqual(1L, wrapper.HandlerCount);
                debouncer.Trigger();
            },
            // T == 2, verify that the seconds handler is not called yet
            () => {
                Assert.AreEqual(1L, wrapper.TriggerCount);
                Assert.AreEqual(1L, wrapper.HandlerCount);
            },
            // T == 3, EventSpacing runs out, second handler invoked
            Skip,
            // T == 4
        ]);
        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task CoalesceDuringHandler()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = 2 * TimingUnit,
            TimingGranularity = 0.1 * TimingUnit,
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        wrapper.Debounced += (s, e) => Sleep(2);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow
            debouncer.Trigger,
            // T == 1,
            Skip,
            // T == 2, DebounceWindow runs out, first handler starts
            Skip,
            // T == 3, in the middle of the handler
            () => {
                debouncer.Trigger();
                debouncer.Trigger();
            },
            // T == 4, handler returns
            Skip,
            // T == 5, DebounceWindow runs out, second handler starts
            Skip,
            // T == 6
            Skip,
            // T == 7, second handler returns
            Skip,
            // T == 8
        ]);
        // Verify
        Assert.AreEqual(3L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
    }
    #endregion

    #region Reset
    [TestMethod]
    public async Task ResetDuringDebounce()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = 2 * TimingUnit
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, the trigger starts the DebounceWindow
            debouncer.Trigger,
            // T == 1, Reset during debounce
            () => {
                Assert.AreEqual(1L, debouncer.Reset());
            },
            // T == 2, DebounceWindow would run out, but current count is 0
            Skip,
            // T == 3
        ]);
        // Verify
        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);
    }

    [TestMethod]
    public async Task ResetFromHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        wrapper.Debounced += (s, e) =>
        {
            if (wrapper.HandlerCount == 1)
            {
                // Trigger again, but Reset before we return from the first handler
                // NOTE: There should never be a second handler invocation.
                debouncer.Trigger();
                Assert.AreEqual(1L, debouncer.Reset());
            }
        };
        await TimedSequence([
            // T == 0, the trigger immediately causes a handler invocation
            debouncer.Trigger,
            // T == 1
        ]);
        // Verify
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }
    #endregion

    [TestMethod]
    public async Task TimingMaximum()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimeSpan.MaxValue,
            TimingGranularity = TimeSpan.MaxValue,
        };
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        await TimedSequence([
            // T == 0, trigger starts maximum DebounceWindow
            debouncer.Trigger,
            // T == 1, reset DebounceWindow and TimingGranularity to 0, causing immediate handler invocation
            () => {
                Assert.AreEqual(0L, wrapper.HandlerCount);
                debouncer.TimingGranularity = TimeSpan.Zero;
                debouncer.DebounceWindow = TimeSpan.Zero;
            }
            // T == 2
        ]);
        // Verify
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }
}
