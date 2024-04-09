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

    Bufferer<MockEvent> debouncer;
    List<IList<MockEvent>> bufferEventsCaptured = new List<IList<MockEvent>>();

    sealed record MockEvent(int Id)
    {
    }

    public BuffererTests()
    {
        debouncer = new Bufferer<MockEvent>();
        debouncer.Buffered += Debouncer_Buffered;
    }

    private void Debouncer_Buffered(object? sender, BufferedEventArgs<MockEvent> e)
    {
        bufferEventsCaptured.Add(e.Events);
    }

    [TestCleanup]
    public void Dispose()
    {
        debouncer.Buffered -= Debouncer_Buffered;
        debouncer.Dispose();
    }

    [TestMethod]
    public void TriggerSingle()
    {
        debouncer.Trigger(new MockEvent(1));
        Sleep(1);
        Assert.AreEqual(1, bufferEventsCaptured.Count);
        Assert.IsTrue(bufferEventsCaptured[0].SequenceEqual([new MockEvent(1)]));
    }

    [TestMethod]
    public void TriggerSingleDelay()
    {
        debouncer.DebounceWindow = TimingUnits(2);
        debouncer.Trigger(new MockEvent(1));
        Sleep(1);
        Assert.AreEqual(0, bufferEventsCaptured.Count);
        Sleep(2);
        Assert.AreEqual(1, bufferEventsCaptured.Count);
        Assert.IsTrue(bufferEventsCaptured[0].SequenceEqual([new MockEvent(1)]));
    }

    [TestMethod]
    public void TriggersWithTimeout()
    {
        debouncer.DebounceWindow = TimingUnits(2);
        debouncer.DebounceTimeout = TimingUnits(4);

        for (var i = 0; i < 6; ++i)
        {
            debouncer.Trigger(new MockEvent(i));
            Sleep(1);
        }
        Assert.AreEqual(1, bufferEventsCaptured.Count);
        Assert.IsTrue(bufferEventsCaptured.Last().SequenceEqual([new MockEvent(0), new MockEvent(1), new MockEvent(2), new MockEvent(3)]), $"Was: [{string.Join(",",bufferEventsCaptured.Last())}]");

        Sleep(2);
        Assert.AreEqual(2, bufferEventsCaptured.Count);
        Assert.IsTrue(bufferEventsCaptured.Last().SequenceEqual([new MockEvent(4), new MockEvent(5)]), $"Was: [{string.Join(",", bufferEventsCaptured.Last())}]");
    }

    [TestMethod]
    public void TriggerCoalescence()
    {
        debouncer.DebounceWindow = TimingUnits(1);
        debouncer.TimingGranularity = TimingUnits(1);
        List<MockEvent> expectedEvents = new();
        for (var i = 0; i < 10; ++i)
        {
            debouncer.Trigger(new MockEvent(i));
            expectedEvents.Add(new MockEvent(i));
        }
        Sleep(4);

        Assert.AreEqual(1, bufferEventsCaptured.Count);
        Assert.IsTrue(bufferEventsCaptured.Last().SequenceEqual(expectedEvents), $"Was: [{string.Join(",", bufferEventsCaptured.Last())}]");
    }

    [TestMethod]
    public void TriggerDuringHandlerSpacing()
    {
        debouncer.HandlerSpacing = TimingUnits(3);

        debouncer.Trigger(new MockEvent(1));
        Sleep(1);
        Assert.AreEqual(1, bufferEventsCaptured.Count);
        Assert.IsTrue(bufferEventsCaptured.Last().SequenceEqual([new MockEvent(1)]));
        debouncer.Trigger(new MockEvent(2));
        Sleep(1);
        Assert.AreEqual(1, bufferEventsCaptured.Count);
        Sleep(2);
        Assert.AreEqual(2, bufferEventsCaptured.Count);
        Assert.IsTrue(bufferEventsCaptured.Last().SequenceEqual([new MockEvent(2)]));
    }

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

        debouncer.Trigger(new MockEvent(1));
        Assert.AreEqual(1L, debouncer.Reset());
        Sleep(2);
        Assert.AreEqual(0L, debouncer.Reset());
    }
    #endregion
}
