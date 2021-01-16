// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics.CodeAnalysis;
using Dorssel.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Testable;

[assembly: CLSCompliant(true)]

namespace TestableUnitTests
{
    [TestClass]
    [TestCategory("Example")]
    [ExcludeFromCodeCoverage]
    public class TestableClassTests
    {
        [TestMethod]
        public void ConstructorHappyFlow()
        {
            var debounce = new Mock<IDebounce>();
            debounce.SetupAdd(m => m.Debounced += It.IsAny<DebouncedEventHandler>());

            using var _ = new TestableClass(debounce.Object);

            debounce.VerifyAdd(m => m.Debounced += It.IsAny<DebouncedEventHandler>(), Times.Once());
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
            debounce.SetupRemove(m => m.Debounced -= It.IsAny<DebouncedEventHandler>());

            using (new TestableClass(debounce.Object)) { }

            debounce.VerifyRemove(m => m.Debounced -= It.IsAny<DebouncedEventHandler>(), Times.Once());
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
            var debouncedEventArgs = new Mock<IDebouncedEventArgs>();
            debouncedEventArgs.SetupGet(m => m.Count).Returns(1);

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, null, debouncedEventArgs.Object);
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
            var debouncedEventArgs = new Mock<IDebouncedEventArgs>();
            debouncedEventArgs.SetupGet(m => m.Count).Returns(0);

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, debouncedEventArgs.Object);
        }

        [TestMethod]
        public void HandlerNegativeCount()
        {
            var debounce = new Mock<IDebounce>();
            var debouncedEventArgs = new Mock<IDebouncedEventArgs>();
            debouncedEventArgs.SetupGet(m => m.Count).Returns(-42);

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, debouncedEventArgs.Object);
        }

        [TestMethod]
        public void HandlerMaxCount()
        {
            var debounce = new Mock<IDebounce>();
            var debouncedEventArgs = new Mock<IDebouncedEventArgs>();
            debouncedEventArgs.SetupGet(m => m.Count).Returns(long.MaxValue);

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, debouncedEventArgs.Object);
        }

        [TestMethod]
        public void HandlerHappyFlow()
        {
            var debounce = new Mock<IDebounce>();
            var debouncedEventArgs = new Mock<IDebouncedEventArgs>();
            debouncedEventArgs.SetupGet(m => m.Count).Returns(1);

            using var _ = new TestableClass(debounce.Object);

            debounce.Raise(m => m.Debounced += null, debounce.Object, debouncedEventArgs.Object);
        }
    }
}
