using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dorssel.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class DebouncerTests
    {
        [TestMethod]
        public void ConstructorNoThrow()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            _ = new Debouncer();
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

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
        public async Task TriggerWithoutHandlers()
        {
            {
                using var debouncer = new Debouncer();
                debouncer.Trigger();
            }
            await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
        }

        [TestMethod]
        public void TriggerAfterDisposeThrows()
        {
            using var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                debouncer.Trigger();
            });
        }

        [TestMethod]
        public async Task TriggerSingle()
        {
            using var debouncer = new Debouncer();
            long callCount = 0;
            debouncer.Debounced += (s, e) =>
            {
                Assert.AreSame(s, debouncer);
                Assert.AreEqual(e.Count, 1L);
                ++callCount;
            };
            debouncer.Trigger();
            await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
            Assert.AreEqual(callCount, 1L);
        }

        [TestMethod]
        public void DebounceIntervalDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(debouncer.DebounceInterval, TimeSpan.Zero);
        }

        public static IEnumerable<object[]> DebounceIntervalValidData
        {
            get
            {
                yield return new object[] { TimeSpan.MaxValue };
                yield return new object[] { TimeSpan.FromMilliseconds(1) };
                yield return new object[] { TimeSpan.FromMilliseconds(0.1) };
                yield return new object[] { TimeSpan.Zero };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(DebounceIntervalValidData), DynamicDataSourceType.Property)]
        public void DebounceIntervalValid(TimeSpan debounceInterval)
        {
            using var debouncer = new Debouncer
            {
                DebounceInterval = debounceInterval
            };
            Assert.AreEqual(debouncer.DebounceInterval, debounceInterval);
        }

        public static IEnumerable<object[]> DebounceIntervalInvalidData
        {
            get
            {
                yield return new object[] { TimeSpan.FromMilliseconds(-1) };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(DebounceIntervalInvalidData), DynamicDataSourceType.Property)]
        public void DebounceIntervalInvalid(TimeSpan debounceInterval)
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.DebounceInterval = debounceInterval);
            Assert.AreEqual(debouncer.DebounceInterval, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public void DebounceIntervalUnchanged()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.AreEqual(debouncer.DebounceInterval, TimeSpan.FromMilliseconds(1));
            debouncer.DebounceInterval = TimeSpan.FromMilliseconds(1);
            Assert.AreEqual(debouncer.DebounceInterval, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public async Task TriggerSingleDelay()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromMilliseconds(100)
            };
            long callCount = 0;
            debouncer.Debounced += (s, e) =>
            {
                Assert.AreSame(s, debouncer);
                Assert.AreEqual(e.Count, 1L);
                ++callCount;
            };
            debouncer.Trigger();
            await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
            Assert.AreEqual(callCount, 0L);
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
            Assert.AreEqual(callCount, 1L);
        }
    }
}
