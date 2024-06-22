// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace UnitTests;

[TestClass]
[TestCategory("Production")]
public class DebouncerTests
{
    #region Constructor
    [TestMethod]
    public void ConstructorDefault()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _ = new Debouncer();
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
        static void Handler(object? sender, DebouncedEventArgs debouncedEventArgs) { }

        using var debouncer = new Debouncer();
        debouncer.Debounced += Handler;
    }
    #endregion

    #region Trigger
    [TestMethod]
    public void TriggerWithoutHandlers()
    {
        using var debouncer = new Debouncer();
        debouncer.Trigger();
    }

    [TestMethod]
    public void TriggerAfterDispose()
    {
        using var debouncer = new Debouncer();
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.Trigger());
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
    #endregion

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
}
