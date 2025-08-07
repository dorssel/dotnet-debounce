// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace TestableUnitTests;

sealed class MockDebouncedEventArgs(long count) : DebouncedEventArgs(count, false)
{
}

[TestClass]
[TestCategory("Example")]
sealed class TestableClassTests
{
    [TestMethod]
    public void ConstructorHappyFlow()
    {
        var debounce = new Mock<IDebouncer>();
        _ = debounce.SetupAdd(m => m.Debounced += It.IsAny<EventHandler<DebouncedEventArgs>>());

        using (new TestableClass(debounce.Object)) { }

        debounce.VerifyAdd(m => m.Debounced += It.IsAny<EventHandler<DebouncedEventArgs>>(), Times.Once());
    }

    [TestMethod]
    public void ConstructorThrowsOnNull()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TestableClass(null!);
        });
    }

    [TestMethod]
    public void DisposeUnregisters()
    {
        var debounce = new Mock<IDebouncer>();
        _ = debounce.SetupRemove(m => m.Debounced -= It.IsAny<EventHandler<DebouncedEventArgs>>());

        using (new TestableClass(debounce.Object)) { }

        debounce.VerifyRemove(m => m.Debounced -= It.IsAny<EventHandler<DebouncedEventArgs>>(), Times.Once());
    }

    [TestMethod]
    public void DisposeNondisposable()
    {
        var debounce = new Mock<IDebouncer>();

        using (new TestableClass(debounce.Object)) { }
    }

    [TestMethod]
    public void DisposeDisposes()
    {
        var debounce = new Mock<IDebouncer>();
        var disposable = debounce.As<IDisposable>();

        using (new TestableClass(debounce.Object)) { }

        disposable.Verify(m => m.Dispose(), Times.Once());
    }

    [TestMethod]
    public void DisposeDisposesOnce()
    {
        var debounce = new Mock<IDebouncer>();
        var disposable = debounce.As<IDisposable>();

        var testable = new TestableClass(debounce.Object);
        testable.Dispose();
        testable.Dispose();

        disposable.Verify(m => m.Dispose(), Times.Once());
    }

    [TestMethod]
    public void HandlerAcceptsNullSender()
    {
        var debounce = new Mock<IDebouncer>();

        using var _ = new TestableClass(debounce.Object);

        debounce.Raise(m => m.Debounced += null, null!, new DebouncedEventArgs(1));
    }

    [TestMethod]
    public void HandlerAcceptsNullEventArgs()
    {
        var debounce = new Mock<IDebouncer>();

        using var _ = new TestableClass(debounce.Object);

        debounce.Raise(m => m.Debounced += null, debounce.Object, null!);
    }

    [TestMethod]
    public void HandlerZeroCount()
    {
        var debounce = new Mock<IDebouncer>();

        using var _ = new TestableClass(debounce.Object);

        debounce.Raise(m => m.Debounced += null, debounce.Object, new MockDebouncedEventArgs(0));
    }

    [TestMethod]
    public void HandlerNegativeCount()
    {
        var debounce = new Mock<IDebouncer>();

        using var _ = new TestableClass(debounce.Object);

        debounce.Raise(m => m.Debounced += null, debounce.Object, new MockDebouncedEventArgs(-42));
    }

    [TestMethod]
    public void HandlerMaxCount()
    {
        var debounce = new Mock<IDebouncer>();

        using var _ = new TestableClass(debounce.Object);

        debounce.Raise(m => m.Debounced += null, debounce.Object, new DebouncedEventArgs(long.MaxValue));
    }

    [TestMethod]
    public void HandlerHappyFlow()
    {
        var debounce = new Mock<IDebouncer>();

        using var _ = new TestableClass(debounce.Object);

        debounce.Raise(m => m.Debounced += null, debounce.Object, new DebouncedEventArgs(1));
    }
}
