// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

namespace UnitTests.Generic;

[TestClass]
[TestCategory("Production")]
sealed class DebouncerGenericTests
{
    #region Constructor
    [TestMethod]
    public void ConstructorDefault()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _ = new Debouncer<int>();
#pragma warning restore CA2000 // Dispose objects before losing scope
    }
    #endregion

    #region DataLimit
    [TestMethod]
    public void DataLimitDefault()
    {
        using var debouncer = new Debouncer<int>();
        Assert.AreEqual(int.MaxValue, debouncer.DataLimit);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(int.MaxValue - 1)]
    [DataRow(int.MaxValue)]
    public void DataLimitValid(int dataLimit)
    {
        using var debouncer = new Debouncer<int>
        {
            DataLimit = 5
        };
        debouncer.DataLimit = dataLimit;
        Assert.AreEqual(dataLimit, debouncer.DataLimit);
    }

    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    public void DataLimitInvalid(int dataLimit)
    {
        using var debouncer = new Debouncer<int>
        {
            DataLimit = 1
        };
        _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
        {
            debouncer.DataLimit = dataLimit;
        });
        Assert.AreEqual(1, debouncer.DataLimit);
    }

    [TestMethod]
    public void DataLimitUnchanged()
    {
        using var debouncer = new Debouncer<int>
        {
            DataLimit = 1
        };
        Assert.AreEqual(1, debouncer.DataLimit);
        debouncer.DataLimit = 1;
        Assert.AreEqual(1, debouncer.DataLimit);
    }

    [TestMethod]
    public void DataLimitAfterDispose()
    {
        var debouncer = new Debouncer<int>();
        debouncer.Dispose();
        _ = Assert.ThrowsException<ObjectDisposedException>(() =>
        {
            debouncer.DataLimit = 1;
        });
    }
    #endregion

    #region EventHandler
    [TestMethod]
    public void EventHandlerAcceptsDebouncedEventArgs()
    {
        static void Handler(object? sender, DebouncedEventArgs<int> debouncedEventArgs) { }

        using var debouncer = new Debouncer<int>();
        debouncer.Debounced += Handler;
    }
    #endregion

    #region Trigger
    [TestMethod]
    public void TriggerWithoutHandlers()
    {
        using var debouncer = new Debouncer<int>();
        debouncer.Trigger(1);
    }

    [TestMethod]
    public void TriggerAfterDispose()
    {
        using var debouncer = new Debouncer<int>();
        debouncer.Dispose();
        _ = Assert.ThrowsException<ObjectDisposedException>(() =>
        {
            debouncer.Trigger(1);
        });
    }

    [TestMethod]
    public async Task TriggerBeyondDataLimitThrows()
    {
        using var debouncer = new Debouncer<int>
        {
            DataLimit = 1,
        };
        using var wrapper = new VerifyingHandlerWrapper<int>(debouncer);
        using var handlerStarted = new SemaphoreSlim(0);
        using var handlerMayFinish = new SemaphoreSlim(0);
        wrapper.Debounced += (s, e) =>
        {
            _ = handlerStarted.Release();
            handlerMayFinish.Wait();
        };

        // T == 0, the first trigger immediately causes a handler invocation
        debouncer.Trigger(1);
        await handlerStarted.WaitAsync();
        // the trigger gets buffered
        debouncer.Trigger(2);

        // the trigger throws
        _ = Assert.ThrowsException<InvalidOperationException>(() =>
        {
            debouncer.Trigger(3);
        });

        // the first handler is released, the second handler is immediately invoked
        _ = handlerMayFinish.Release();
        await handlerStarted.WaitAsync();
        _ = handlerMayFinish.Release();
        await debouncer.CurrentEventHandlersTask.WaitAsync(CancellationToken.None);

        // Verify
        Assert.AreEqual(2L, wrapper.TriggerCount);
        Assert.AreEqual(2L, wrapper.HandlerCount);
        CollectionAssert.That.AreEqual([1, 2], wrapper.TriggerData);
        CollectionAssert.That.AreEqual([2], wrapper.LastTriggerData);
    }
    #endregion

    #region Reset
    [TestMethod]
    public void ResetWhileIdle()
    {
        using var debouncer = new Debouncer<int>();
        Assert.AreEqual(0L, debouncer.Reset(out var data));
        CollectionAssert.That.AreEqual([], data);
    }

    [TestMethod]
    public void ResetAfterDispose()
    {
        using var debouncer = new Debouncer<int>();
        debouncer.Dispose();
        Assert.AreEqual(0L, debouncer.Reset(out var data));
        CollectionAssert.That.AreEqual([], data);
    }
    #endregion
}
