// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using Void = Dorssel.Utilities.Void;

namespace UnitTests;

[TestClass]
[TestCategory("Production")]
public class DebouncerTests
{
    static TimeSpan TimingUnits(double count) => TimeSpan.FromMilliseconds(100 * count);

    static void Sleep(double count) => Thread.Sleep(TimingUnits(count));

    #region Constructor
    [TestMethod]
    public void ConstructorDefault()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _ = new Debouncer();
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    [TestMethod]
    public void ConstructorDefaultGeneric()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _ = new Debouncer<int>();
#pragma warning restore CA2000 // Dispose objects before losing scope
    }
    #endregion

    #region Dispose
    [TestMethod]
    public void DisposeNoThrow()
    {
        var debouncer = new Debouncer();
        debouncer.Dispose();
    }

    [TestMethod]
    public void DisposeMultipleNoThrow()
    {
        var debouncer = new Debouncer();
        debouncer.Dispose();
        debouncer.Dispose();
    }

    [TestMethod]
    public void DisposeDuringTimer()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(2)
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        debouncer.Trigger();
        Sleep(1);
        debouncer.Dispose();
        Assert.AreEqual(0L, wrapper.HandlerCount);
    }

    [TestMethod]
    public void DisposeDuringHandler()
    {
        var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        using var done = new ManualResetEventSlim();
        wrapper.Debounced += (s, e) =>
        {
            Sleep(2);
            done.Set();
        };
        debouncer.Trigger();
        Sleep(1);
        debouncer.Dispose();
        Assert.IsTrue(done.Wait(TimingUnits(2)));
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }

    [TestMethod]
    public void DisposeFromHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        using var done = new ManualResetEventSlim();
        wrapper.Debounced += (s, e) =>
        {
            debouncer.Dispose();
            done.Set();
        };
        debouncer.Trigger();
        Assert.IsTrue(done.Wait(TimingUnits(1)));
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }
    #endregion

    #region DebounceWindow
    [TestMethod]
    public void DebounceWindowDefault()
    {
        using var debouncer = new Debouncer();
        Assert.AreEqual(TimeSpan.Zero, debouncer.DebounceWindow);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    public void DebounceWindowValid(TimeSpan DebounceWindow)
    {
        using var debouncer = new Debouncer
        {
            DebounceWindow = TimeSpanData.ArbitraryNonDefault
        };
        debouncer.DebounceWindow = DebounceWindow;
        Assert.AreEqual(DebounceWindow, debouncer.DebounceWindow);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void DebounceWindowInvalid(TimeSpan DebounceWindow)
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimeSpan.FromMilliseconds(1)
        };
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.DebounceWindow = DebounceWindow);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceWindow);
    }

    [TestMethod]
    public void DebounceWindowUnchanged()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimeSpan.FromMilliseconds(1)
        };
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceWindow);
        debouncer.DebounceWindow = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceWindow);
    }

    [TestMethod]
    public void DebounceWindowAfterDispose()
    {
        var debouncer = new Debouncer();
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceWindow = TimeSpan.Zero);
    }
    #endregion

    #region DebounceTimeout
    [TestMethod]
    public void DebounceTimeoutDefault()
    {
        using var debouncer = new Debouncer();
        Assert.AreEqual(Timeout.InfiniteTimeSpan, debouncer.DebounceTimeout);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void DebounceTimeoutValid(TimeSpan debounceTimeout)
    {
        using var debouncer = new Debouncer
        {
            DebounceTimeout = TimeSpanData.ArbitraryNonDefault
        };
        debouncer.DebounceTimeout = debounceTimeout;
        Assert.AreEqual(debounceTimeout, debouncer.DebounceTimeout);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    public void DebounceTimeoutInvalid(TimeSpan debounceTimeout)
    {
        using var debouncer = new Debouncer()
        {
            DebounceTimeout = TimeSpan.FromMilliseconds(1)
        };
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.DebounceTimeout = debounceTimeout);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceTimeout);
    }

    [TestMethod]
    public void DebounceTimeoutUnchanged()
    {
        using var debouncer = new Debouncer()
        {
            DebounceTimeout = TimeSpan.FromMilliseconds(1)
        };
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceTimeout);
        debouncer.DebounceTimeout = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceTimeout);
    }

    [TestMethod]
    public void DebounceTimeoutAfterDispose()
    {
        var debouncer = new Debouncer();
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceTimeout = TimeSpan.Zero);
    }
    #endregion

    #region EventSpacing
    [TestMethod]
    public void EventSpacingDefault()
    {
        using var debouncer = new Debouncer();
        Assert.AreEqual(TimeSpan.Zero, debouncer.EventSpacing);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    public void EventSpacingValid(TimeSpan eventSpacing)
    {
        using var debouncer = new Debouncer
        {
            EventSpacing = TimeSpanData.ArbitraryNonDefault
        };
        debouncer.EventSpacing = eventSpacing;
        Assert.AreEqual(eventSpacing, debouncer.EventSpacing);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void EventSpacingInvalid(TimeSpan eventSpacing)
    {
        using var debouncer = new Debouncer()
        {
            EventSpacing = TimeSpan.FromMilliseconds(1)
        };
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.EventSpacing = eventSpacing);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.EventSpacing);
    }

    [TestMethod]
    public void EventSpacingUnchanged()
    {
        using var debouncer = new Debouncer()
        {
            EventSpacing = TimeSpan.FromMilliseconds(1)
        };
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.EventSpacing);
        debouncer.EventSpacing = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.EventSpacing);
    }

    [TestMethod]
    public void EventSpacingAfterDispose()
    {
        var debouncer = new Debouncer();
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.EventSpacing = TimeSpan.Zero);
    }
    #endregion

    #region HandlerSpacing
    [TestMethod]
    public void HandlerSpacingDefault()
    {
        using var debouncer = new Debouncer();
        Assert.AreEqual(TimeSpan.Zero, debouncer.HandlerSpacing);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    public void HandlerSpacingValid(TimeSpan HandlerSpacing)
    {
        using var debouncer = new Debouncer
        {
            HandlerSpacing = TimeSpanData.ArbitraryNonDefault
        };
        debouncer.HandlerSpacing = HandlerSpacing;
        Assert.AreEqual(HandlerSpacing, debouncer.HandlerSpacing);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void HandlerSpacingInvalid(TimeSpan HandlerSpacing)
    {
        using var debouncer = new Debouncer()
        {
            HandlerSpacing = TimeSpan.FromMilliseconds(1)
        };
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.HandlerSpacing = HandlerSpacing);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.HandlerSpacing);
    }

    [TestMethod]
    public void HandlerSpacingUnchanged()
    {
        using var debouncer = new Debouncer()
        {
            HandlerSpacing = TimeSpan.FromMilliseconds(1)
        };
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.HandlerSpacing);
        debouncer.HandlerSpacing = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.HandlerSpacing);
    }

    [TestMethod]
    public void HandlerSpacingAfterDispose()
    {
        var debouncer = new Debouncer();
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.HandlerSpacing = TimeSpan.Zero);
    }
    #endregion

    #region TimingGranularity
    [TestMethod]
    public void TimingGranularityDefault()
    {
        using var debouncer = new Debouncer();
        Assert.AreEqual(TimeSpan.Zero, debouncer.TimingGranularity);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    public void TimingGranularityValid(TimeSpan timingGranularity)
    {
        using var debouncer = new Debouncer
        {
            DebounceWindow = TimeSpan.MaxValue,
            TimingGranularity = TimeSpanData.ArbitraryNonDefault
        };
        debouncer.TimingGranularity = timingGranularity;
        Assert.AreEqual(timingGranularity, debouncer.TimingGranularity);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void TimingGranularityInvalid(TimeSpan timingGranularity)
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimeSpan.MaxValue,
            TimingGranularity = TimeSpan.FromMilliseconds(2)
        };
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.TimingGranularity = timingGranularity);
        Assert.AreEqual(TimeSpan.FromMilliseconds(2), debouncer.TimingGranularity);
    }

    [TestMethod]
    public void TimingGranularityUnchanged()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimeSpan.MaxValue,
            TimingGranularity = TimeSpan.FromMilliseconds(2)
        };
        Assert.AreEqual(TimeSpan.FromMilliseconds(2), debouncer.TimingGranularity);
        debouncer.TimingGranularity = TimeSpan.FromMilliseconds(2);
        Assert.AreEqual(TimeSpan.FromMilliseconds(2), debouncer.TimingGranularity);
    }

    [TestMethod]
    public void TimingGranularityAfterDispose()
    {
        var debouncer = new Debouncer();
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.TimingGranularity = TimeSpan.Zero);
    }
    #endregion

    #region EventHandler
    [TestMethod]
    public void EventHandlerAcceptsDebouncedEventArgs()
    {
        static void Handler(object? sender, DebouncedEventArgs<Void> debouncedEventArgs) { }

        using var debouncer = new Debouncer();
        debouncer.Debounced += Handler;
    }
    #endregion

    #region Trigger
    [TestMethod]
    public void TriggerWithoutHandlers()
    {
        {
            using var debouncer = new Debouncer();
            debouncer.Trigger();
        }
        Sleep(1);
    }

    [TestMethod]
    public void TriggerAfterDispose()
    {
        using var debouncer = new Debouncer();
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.Trigger());
    }

    [TestMethod]
    public void TriggerSingle()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggerSingleGeneric()
    {
        using var debouncer = new Debouncer<int>();
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        debouncer.Trigger(1);
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }

    [TestMethod]
    public void TriggerSingleDelay()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(2)
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
        Sleep(2);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggerSingleDelayGeneric()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = TimingUnits(2)
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        debouncer.Trigger(99);
        Sleep(1);
        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
        Sleep(2);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([99]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([99]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }

    [TestMethod]
    public void TriggersWithTimeout()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(2),
            DebounceTimeout = TimingUnits(4)
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        for (var i = 0; i < 6; ++i)
        {
            debouncer.Trigger();
            Sleep(1);
        }
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Sleep(2);
        Assert.AreEqual(6L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggersWithTimeoutGeneric()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = TimingUnits(2),
            DebounceTimeout = TimingUnits(4)
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        for (var i = 0; i < 6; ++i)
        {
            debouncer.Trigger(i);
            Sleep(1);
        }
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Sleep(2);
        Assert.AreEqual(6L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([0, 1, 2, 3, 4, 5]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([4, 5]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }

    [TestMethod]
    public void TriggerCoalescence()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(1),
            TimingGranularity = TimingUnits(1)
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        for (var i = 0; i < 10; ++i)
        {
            debouncer.Trigger();
        }
        Sleep(4);
        Assert.AreEqual(10L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggerCoalescenceGeneric()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = TimingUnits(1),
            TimingGranularity = TimingUnits(1)
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        for (var i = 0; i < 10; ++i)
        {
            debouncer.Trigger(i);
        }
        Sleep(4);
        Assert.AreEqual(10L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }

    [TestMethod]
    public void TriggerFromHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);

        wrapper.Debounced += (s, e) =>
        {
            if (wrapper.HandlerCount == 1)
            {
                debouncer.Trigger();
            }
        };
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggerFromHandlerGeneric()
    {
        using var debouncer = new Debouncer<int>();
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);

        wrapper.Debounced += (s, e) =>
        {
            if (wrapper.HandlerCount == 1)
            {
                debouncer.Trigger(99);
            }
        };
        debouncer.Trigger(1);
        Sleep(1);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1, 99]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([99]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }

    [TestMethod]
    public void TriggerDuringHandlerSpacing()
    {
        using var debouncer = new Debouncer()
        {
            HandlerSpacing = TimingUnits(3)
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
        Sleep(2);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggerDuringHandlerSpacingGeneric()
    {
        using var debouncer = new Debouncer<int>()
        {
            HandlerSpacing = TimingUnits(3)
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        debouncer.Trigger(1);
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
        debouncer.Trigger(2);
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
        Sleep(2);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1, 2]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([2]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }

    [TestMethod]
    public void TriggerDuringEventSpacing()
    {
        using var debouncer = new Debouncer()
        {
            EventSpacing = TimingUnits(3)
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
        Sleep(2);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggerDuringEventSpacingGeneric()
    {
        using var debouncer = new Debouncer<int>()
        {
            EventSpacing = TimingUnits(3)
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        debouncer.Trigger(1);
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
        debouncer.Trigger(2);
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
        Sleep(2);
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1, 2]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([2]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }

    [TestMethod]
    public void CoalesceDuringHandler()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(1),
            TimingGranularity = TimingUnits(0.1),
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        wrapper.Debounced += (s, e) => Sleep(2);
        debouncer.Trigger();
        Sleep(2);
        debouncer.Trigger();
        debouncer.Trigger();
        Sleep(5);
        Assert.AreEqual(3L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void CoalesceDuringHandlerGeneric()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = TimingUnits(1),
            TimingGranularity = TimingUnits(0.1),
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        wrapper.Debounced += (s, e) => Sleep(2);
        debouncer.Trigger(1);
        Sleep(2);
        debouncer.Trigger(2);
        debouncer.Trigger(3);
        Sleep(5);
        Assert.AreEqual(3L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1, 2, 3]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([2, 3]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }

    [TestMethod]
    public void TriggersWithTriggerCount()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(2),
            DebounceAfterTriggerCount = 4
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        for (var i = 0; i < 6; ++i)
        {
            debouncer.Trigger();
            Sleep(1);
        }
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(4L, wrapper.TriggerCount);
        Sleep(2);
        Assert.AreEqual(6L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggersWithTimeoutAndTriggerCountAndTimeoutWins()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(2),
            DebounceTimeout = TimingUnits(4),
            DebounceAfterTriggerCount = 5
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        for (var i = 0; i < 6; ++i)
        {
            debouncer.Trigger();
            Sleep(1);
        }
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(4L, wrapper.TriggerCount);
        Sleep(2);
        Assert.AreEqual(6L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void TriggersWithTimeoutTriggerCountAndTriggerCountWins()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(2),
            DebounceTimeout = TimingUnits(5),
            DebounceAfterTriggerCount = 4
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        for (var i = 0; i < 6; ++i)
        {
            debouncer.Trigger();
            Sleep(1);
        }
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(4L, wrapper.TriggerCount);
        Sleep(2);
        Assert.AreEqual(6L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }
    #endregion

    #region Reset
    [TestMethod]
    public void ResetWhileIdle()
    {
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(0L, debouncer.Reset());
        }
    }

    [TestMethod]
    public void ResetAfterDispose()
    {
        using var debouncer = new Debouncer();
        debouncer.Dispose();
        Assert.AreEqual(0L, debouncer.Reset());
    }

    [TestMethod]
    public void ResetDuringDebounce()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimingUnits(1)
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        debouncer.Trigger();
        Assert.AreEqual(1L, debouncer.Reset());
        Sleep(2);
        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);
    }

    [TestMethod]
    public void ResetDuringDebounceGeneric()
    {
        using var debouncer = new Debouncer<int>()
        {
            DebounceWindow = TimingUnits(1)
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        debouncer.Trigger(1);
        Assert.AreEqual(1L, debouncer.Reset());
        Sleep(2);
        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);
        Assert.AreEqual(0L, wrapper.TriggerData.Count);
        Assert.AreEqual(0L, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void ResetFromHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);

        wrapper.Debounced += (s, e) =>
        {
            if (wrapper.HandlerCount == 1)
            {
                debouncer.Trigger();
                Assert.AreEqual(1L, debouncer.Reset());
            }
        };
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.AreEqual(0, wrapper.TriggerData.Count);
        Assert.AreEqual(0, wrapper.LastTriggerData.Count);
    }

    [TestMethod]
    public void ResetFromHandlerGeneric()
    {
        using var debouncer = new Debouncer<int>();
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);

        wrapper.Debounced += (s, e) =>
        {
            if (wrapper.HandlerCount == 1)
            {
                debouncer.Trigger(2);
                Assert.AreEqual(1L, debouncer.Reset());
            }
        };
        debouncer.Trigger(1);
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        Assert.IsTrue(wrapper.TriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.TriggerData)}]");
        Assert.IsTrue(wrapper.LastTriggerData.SequenceEqual([1]), $"Was: [{string.Join(",", wrapper.LastTriggerData)}]");
    }
    #endregion

    [TestMethod]
    public void TimingMaximum()
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = TimeSpan.MaxValue,
            TimingGranularity = TimeSpan.MaxValue,
        };
        using var wrapper = new VerifyingHandlerWrapper<Void>(debouncer);
        debouncer.Trigger();
        Sleep(1);
        Assert.AreEqual(0L, wrapper.HandlerCount);
        debouncer.TimingGranularity = TimeSpan.Zero;
        debouncer.DebounceWindow = TimeSpan.Zero;
        Sleep(1);
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
    }

    [TestMethod]
    public void BenchmarkDefaults()
    {
        using var debouncer = new Debouncer();
        var benchmark = debouncer.Benchmark;
        Assert.AreEqual(0L, benchmark.HandlersCalled);
        Assert.AreEqual(0L, benchmark.TriggersReported);
        Assert.AreEqual(0L, benchmark.RescheduleCount);
        Assert.AreEqual(0L, benchmark.TimerChanges);
        Assert.AreEqual(0L, benchmark.TimerEvents);
    }

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(1, 0, 1)]
    [DataRow(long.MaxValue - 1, 0, long.MaxValue - 1)]
    [DataRow(long.MaxValue, 0, long.MaxValue)]
    [DataRow(0, 1, 1)]
    [DataRow(1, 1, 2)]
    [DataRow(long.MaxValue - 1, 1, long.MaxValue)]
    [DataRow(long.MaxValue, 1, long.MaxValue)]
    [DataRow(0, long.MaxValue - 1, long.MaxValue - 1)]
    [DataRow(1, long.MaxValue - 1, long.MaxValue)]
    [DataRow(long.MaxValue - 1, long.MaxValue - 1, long.MaxValue)]
    [DataRow(long.MaxValue, long.MaxValue - 1, long.MaxValue)]
    [DataRow(0, long.MaxValue, long.MaxValue)]
    [DataRow(1, long.MaxValue, long.MaxValue)]
    [DataRow(long.MaxValue - 1, long.MaxValue, long.MaxValue)]
    [DataRow(long.MaxValue, long.MaxValue, long.MaxValue)]
    public void AddWithClamp(long left, long right, long expected)
    {
        Assert.AreEqual(expected, Debouncer.AddWithClamp(left, right));
    }
}
