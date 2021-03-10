// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics.CodeAnalysis;
using Dorssel.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Testable;

[assembly: CLSCompliant(true)]

namespace TestableUnitTests
{
    class MockDebouncedEventArgs : DebouncedEventArgs
    {
        public MockDebouncedEventArgs(long count)
            : base(count, false)
        {
        }
    }

    [TestClass]
    [TestCategory("Example")]
    [ExcludeFromCodeCoverage]
    public class TestableClassTests
    {
        [TestMethod]
        public void ConstructorHappyFlow()
        {
            var debounce = new Mock<IDebounce>();
            debounce.SetupAdd(m => m.Debounced += It.IsAny<EventHandler<DebouncedEventArgs>>());

            using var _ = new TestableClass(debounce.Object);

            debounce.VerifyAdd(m => m.Debounced += It.IsAny<EventHandler<DebouncedEventArgs>>(), Times.Once());
        }

        [TestMethod]
        public void ConstructorThrowsOnNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                using var _ = new TestableClass(null!);
            });
        }

        [TestMethod]
        public void DisposeUnregisters()
        {
            var debounce = new Mock<IDebounce>();
            debounce.SetupRemove(m => m.Debounced -= It.IsAny<EventHandler<DebouncedEventArgs>>());

            using (new TestableClass(debounce.Object)) { }

            debounce.VerifyRemove(m => m.Debounced -= It.IsAny<EventHandler<DebouncedEventArgs>>(), Times.Once());
        }


        [TestMethod]
        public void DisposeDisposes()
        {
            var debounce = new Mock<IDebounce>();

            using (new TestableClass(debounce.Object)) { }

            debounce.Verify(m => m.Dispose());
        }

        [TestMethod]
        public void DisposeDisposesOnce()
        {
            var debounce = new Mock<IDebounce>();

            var testable = new TestableClass(debounce.Object);
            testable.Dispose();
            testable.Dispose();

            debounce.Verify(m => m.Dispose(), Times.Once());
        }

        [TestMethod]
        public void HandlerAcceptsNullSender()
        {
            var debounce = new Mock<IDebounce>();

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, null, new DebouncedEventArgs(1));
        }

        [TestMethod]
        public void HandlerAcceptsNullEventArgs()
        {
            var debounce = new Mock<IDebounce>();

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, null);
        }

        [TestMethod]
        public void HandlerZeroCount()
        {
            var debounce = new Mock<IDebounce>();

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, new MockDebouncedEventArgs(0));
        }

        [TestMethod]
        public void HandlerNegativeCount()
        {
            var debounce = new Mock<IDebounce>();

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, new MockDebouncedEventArgs(-42));
        }

        [TestMethod]
        public void HandlerMaxCount()
        {
            var debounce = new Mock<IDebounce>();

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, new DebouncedEventArgs(long.MaxValue));
        }

        [TestMethod]
        public void HandlerHappyFlow()
        {
            var debounce = new Mock<IDebounce>();

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, new DebouncedEventArgs(1));
        }
    }
}
