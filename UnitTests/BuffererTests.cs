// SPDX-FileCopyrightText: 2024 Alain van den Berg
//
// SPDX-License-Identifier: MIT

namespace UnitTests;

[TestClass]
[TestCategory("Production")]
public sealed class BuffererTests : IDisposable
{
    static TimeSpan TimingUnits(double count) => TimeSpan.FromMilliseconds(50 * count);

    static void Sleep(double count) => Thread.Sleep(TimingUnits(count));

    Bufferer<int> debouncer;
    List<IReadOnlyList<int>> buffersCaptured = new();

    public BuffererTests()
    {
        debouncer = new Bufferer<int>();
        debouncer.Buffered += Debouncer_Buffered;
    }

    private void Debouncer_Buffered(object? sender, BufferedEventArgs<int> e)
    {
        buffersCaptured.Add(e.Buffer);
    }

    [TestCleanup]
    public void Dispose()
    {
        debouncer.Buffered -= Debouncer_Buffered;
        debouncer.Dispose();
    }

    #region Dispose
    [TestMethod]
    public void DisposeNoThrow()
    {
        debouncer.Dispose();
    }

    [TestMethod]
    public void DisposeMultipleNoThrow()
    {
        debouncer.Dispose();
        debouncer.Dispose();
    }
    #endregion

    #region Trigger
    [TestMethod]
    public void TriggerWithoutHandler()
    {
        debouncer.Trigger(1);
        Sleep(1);
    }

    [TestMethod]
    public void TriggerSingle()
    {
        debouncer.Trigger(1);
        Sleep(1);
        Assert.AreEqual(1, buffersCaptured.Count);
        Assert.IsTrue(buffersCaptured[0].SequenceEqual([1]));
    }

    [TestMethod]
    public void TriggerSingleDelay()
    {
        debouncer.DebounceWindow = TimingUnits(2);
        debouncer.Trigger(1);
        Sleep(1);
        Assert.AreEqual(0, buffersCaptured.Count);
        Sleep(2);
        Assert.AreEqual(1, buffersCaptured.Count);
        Assert.IsTrue(buffersCaptured[0].SequenceEqual([1]));
    }

    [TestMethod]
    public void TriggersWithTimeout()
    {
        debouncer.DebounceWindow = TimingUnits(2);
        debouncer.DebounceTimeout = TimingUnits(4);

        for (var i = 0; i < 6; ++i)
        {
            debouncer.Trigger(i);
            Sleep(1);
        }
        Assert.AreEqual(1, buffersCaptured.Count);
        Assert.IsTrue(buffersCaptured.Last().SequenceEqual([0, 1, 2, 3]), $"Was: [{string.Join(",",buffersCaptured.Last())}]");

        Sleep(2);
        Assert.AreEqual(2, buffersCaptured.Count);
        Assert.IsTrue(buffersCaptured.Last().SequenceEqual([4, 5]), $"Was: [{string.Join(",", buffersCaptured.Last())}]");
    }

    [TestMethod]
    public void TriggerCoalescence()
    {
        debouncer.DebounceWindow = TimingUnits(1);
        debouncer.TimingGranularity = TimingUnits(1);
        List<int> expectedEvents = new();
        for (var i = 0; i < 10; ++i)
        {
            debouncer.Trigger(i);
            expectedEvents.Add(i);
        }
        Sleep(4);

        Assert.AreEqual(1, buffersCaptured.Count);
        Assert.IsTrue(buffersCaptured.Last().SequenceEqual(expectedEvents), $"Was: [{string.Join(",", buffersCaptured.Last())}]");
    }

    [TestMethod]
    public void TriggerDuringHandlerSpacing()
    {
        debouncer.HandlerSpacing = TimingUnits(3);

        debouncer.Trigger(1);
        Sleep(1);
        Assert.AreEqual(1, buffersCaptured.Count);
        Assert.IsTrue(buffersCaptured.Last().SequenceEqual([1]));
        debouncer.Trigger(2);
        Sleep(1);
        Assert.AreEqual(1, buffersCaptured.Count);
        Sleep(2);
        Assert.AreEqual(2, buffersCaptured.Count);
        Assert.IsTrue(buffersCaptured.Last().SequenceEqual([2]));
    }
    #endregion

    #region Reset
    [TestMethod]
    public void ResetWhileIdle()
    {
        Assert.AreEqual(0L, debouncer.Reset());
    }

    [TestMethod]
    public void ResetAfterDispose()
    {
        debouncer.Dispose();
        Assert.AreEqual(0L, debouncer.Reset());
    }

    [TestMethod]
    public void ResetDuringDebounce()
    {
        debouncer.DebounceWindow = TimingUnits(1);

        debouncer.Trigger(1);
        Assert.AreEqual(1L, debouncer.Reset());
        Sleep(2);
        Assert.AreEqual(0L, debouncer.Reset());
    }
    #endregion

    [TestMethod]
    public void ConstructorWithNullDebouncer()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new Bufferer<int>(null!));
    }

    #region DebounceTimeout
    [TestMethod]
    public void DebounceTimeoutDefault()
    {
        Assert.AreEqual(Timeout.InfiniteTimeSpan, debouncer.DebounceTimeout);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void DebounceTimeoutValid(TimeSpan debounceTimeout)
    {
        debouncer.DebounceTimeout = TimeSpanData.ArbitraryNonDefault;

        debouncer.DebounceTimeout = debounceTimeout;
        Assert.AreEqual(debounceTimeout, debouncer.DebounceTimeout);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    public void DebounceTimeoutInvalid(TimeSpan debounceTimeout)
    {
        debouncer.DebounceTimeout = TimeSpan.FromMilliseconds(1);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.DebounceTimeout = debounceTimeout);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceTimeout);
    }

    [TestMethod]
    public void DebounceTimeoutUnchanged()
    {
        debouncer.DebounceTimeout = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceTimeout);
        debouncer.DebounceTimeout = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceTimeout);
    }

    [TestMethod]
    public void DebounceTimeoutAfterDispose()
    {
        var debouncer = new Bufferer<int>();
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceTimeout = TimeSpan.Zero);
    }
    #endregion

    #region EventSpacing
    [TestMethod]
    public void EventSpacingDefault()
    {
        Assert.AreEqual(TimeSpan.Zero, debouncer.EventSpacing);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    public void EventSpacingValid(TimeSpan eventSpacing)
    {
        debouncer.EventSpacing = TimeSpanData.ArbitraryNonDefault;

        debouncer.EventSpacing = eventSpacing;
        Assert.AreEqual(eventSpacing, debouncer.EventSpacing);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void EventSpacingInvalid(TimeSpan eventSpacing)
    {
        debouncer.EventSpacing = TimeSpan.FromMilliseconds(1);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.EventSpacing = eventSpacing);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.EventSpacing);
    }

    [TestMethod]
    public void EventSpacingUnchanged()
    {
        debouncer.EventSpacing = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.EventSpacing);
        debouncer.EventSpacing = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.EventSpacing);
    }

    [TestMethod]
    public void EventSpacingAfterDispose()
    {
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.EventSpacing = TimeSpan.Zero);
    }
    #endregion

    #region HandlerSpacing
    [TestMethod]
    public void HandlerSpacingDefault()
    {
        Assert.AreEqual(TimeSpan.Zero, debouncer.HandlerSpacing);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    public void HandlerSpacingValid(TimeSpan HandlerSpacing)
    {
        debouncer.HandlerSpacing = TimeSpanData.ArbitraryNonDefault;

        debouncer.HandlerSpacing = HandlerSpacing;
        Assert.AreEqual(HandlerSpacing, debouncer.HandlerSpacing);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void HandlerSpacingInvalid(TimeSpan HandlerSpacing)
    {
        debouncer.HandlerSpacing = TimeSpan.FromMilliseconds(1);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.HandlerSpacing = HandlerSpacing);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.HandlerSpacing);
    }

    [TestMethod]
    public void HandlerSpacingUnchanged()
    {
        debouncer.HandlerSpacing = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.HandlerSpacing);
        debouncer.HandlerSpacing = TimeSpan.FromMilliseconds(1);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.HandlerSpacing);
    }

    [TestMethod]
    public void HandlerSpacingAfterDispose()
    {
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.HandlerSpacing = TimeSpan.Zero);
    }
    #endregion

    #region TimingGranularity
    [TestMethod]
    public void TimingGranularityDefault()
    {
        Assert.AreEqual(TimeSpan.Zero, debouncer.TimingGranularity);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.NonNegative), typeof(TimeSpanData))]
    public void TimingGranularityValid(TimeSpan timingGranularity)
    {
        debouncer.DebounceWindow = TimeSpan.MaxValue;
        debouncer.TimingGranularity = TimeSpanData.ArbitraryNonDefault;

        debouncer.TimingGranularity = timingGranularity;
        Assert.AreEqual(timingGranularity, debouncer.TimingGranularity);
    }

    [TestMethod]
    [DynamicData(nameof(TimeSpanData.Negative), typeof(TimeSpanData))]
    [DynamicData(nameof(TimeSpanData.Infinite), typeof(TimeSpanData))]
    public void TimingGranularityInvalid(TimeSpan timingGranularity)
    {
        debouncer.DebounceWindow = TimeSpan.MaxValue;
        debouncer.TimingGranularity = TimeSpan.FromMilliseconds(2);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.TimingGranularity = timingGranularity);
        Assert.AreEqual(TimeSpan.FromMilliseconds(2), debouncer.TimingGranularity);
    }

    [TestMethod]
    public void TimingGranularityUnchanged()
    {
        debouncer.DebounceWindow = TimeSpan.MaxValue;
        debouncer.TimingGranularity = TimeSpan.FromMilliseconds(2);

        Assert.AreEqual(TimeSpan.FromMilliseconds(2), debouncer.TimingGranularity);
        debouncer.TimingGranularity = TimeSpan.FromMilliseconds(2);
        Assert.AreEqual(TimeSpan.FromMilliseconds(2), debouncer.TimingGranularity);
    }

    [TestMethod]
    public void TimingGranularityAfterDispose()
    {
        debouncer.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => debouncer.TimingGranularity = TimeSpan.Zero);
    }
    #endregion
}
