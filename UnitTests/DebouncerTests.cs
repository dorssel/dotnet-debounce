// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace UnitTests;

[TestClass]
[TestCategory("Production")]
sealed class DebouncerTests
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

    /// <summary>
    /// Verify that Dispose() can be called safely while a handler is running.
    /// </summary>
    [TestMethod]
    public async Task DisposeDuringHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        using var started = new SemaphoreSlim(0);
        using var finish = new SemaphoreSlim(0);
        wrapper.Debounced += (s, e) =>
        {
            _ = started.Release();
            finish.Wait(CancellationToken.None);
        };

        // the trigger immediately causes a handler invocation
        debouncer.Trigger();
        await started.WaitAsync(CancellationToken.None);
        // in the middle of the handler
        debouncer.Dispose();
        _ = finish.Release();
        // handler exits
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(debouncer.Trigger);
    }

    /// <summary>
    /// Verify that Dispose() can be called safely from within a handler.
    /// </summary>
    [TestMethod]
    public async Task DisposeFromHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);
        wrapper.Debounced += (s, e) => debouncer.Dispose();

        // the trigger immediately causes a handler invocation
        debouncer.Trigger();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(1L, wrapper.TriggerCount);
        Assert.AreEqual(1L, wrapper.HandlerCount);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(debouncer.Trigger);
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
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => debouncer.DebounceWindow = DebounceWindow);
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
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => debouncer.DebounceWindow = TimeSpan.Zero);
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
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => debouncer.DebounceTimeout = debounceTimeout);
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
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => debouncer.DebounceTimeout = TimeSpan.Zero);
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
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => debouncer.EventSpacing = eventSpacing);
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
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => debouncer.EventSpacing = TimeSpan.Zero);
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
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => debouncer.HandlerSpacing = HandlerSpacing);
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
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => debouncer.HandlerSpacing = TimeSpan.Zero);
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
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => debouncer.TimingGranularity = timingGranularity);
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
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => debouncer.TimingGranularity = TimeSpan.Zero);
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
        _ = Assert.ThrowsExactly<ObjectDisposedException>(debouncer.Trigger);
    }

    [TestMethod]
    public async Task TriggerSingle()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        // the trigger immediately causes a handler invocation
        debouncer.Trigger();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(1L, wrapper.TriggerCount);
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

        // the trigger immediately causes a handler invocation
        debouncer.Trigger();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);
        // the handler triggered, which immediately causes another handler invocation
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
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
    public async Task ResetFromHandler()
    {
        using var debouncer = new Debouncer();
        using var wrapper = new VerifyingHandlerWrapper(debouncer);

        wrapper.Debounced += (s, e) =>
        {
            if (wrapper.HandlerCount == 1)
            {
                // Trigger again, but Reset before we return from the first handler
                debouncer.Trigger();
                Assert.AreEqual(1L, debouncer.Reset());
            }
        };

        // the trigger immediately causes a handler invocation
        debouncer.Trigger();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);
        // there should never be a second handler invocation
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

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

        // trigger starts maximum DebounceWindow
        debouncer.Trigger();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        Assert.AreEqual(0L, wrapper.TriggerCount);
        Assert.AreEqual(0L, wrapper.HandlerCount);

        // reset DebounceWindow and TimingGranularity to 0, causing immediate handler invocation
        debouncer.TimingGranularity = TimeSpan.Zero;
        debouncer.DebounceWindow = TimeSpan.Zero;
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
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
}
