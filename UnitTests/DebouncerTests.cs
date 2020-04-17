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
        public void NoCrashCreate()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            _ = new Debouncer();
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        [TestMethod]
        public void NoCrashDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
        }

        [TestMethod]
        public void NoCrashDisposeMultiple()
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
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        }

        [TestMethod]
        public void TriggerAfterDisposeThrows()
        {
            using var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => {
                debouncer.Trigger();
            });
        }

        [TestMethod]
        public async Task SingleTrigger()
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
        public void DefaultMinimumDebounceTime()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(debouncer.MinimumDebounceTime, TimeSpan.Zero);
        }

        public static IEnumerable<object[]> ValidMinimumDebounceTimes {
            get {
                yield return new object[] { TimeSpan.Zero };
                yield return new object[] { TimeSpan.FromMilliseconds(0.1) };
                yield return new object[] { TimeSpan.FromMilliseconds(1) };
                yield return new object[] { TimeSpan.MaxValue };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ValidMinimumDebounceTimes), DynamicDataSourceType.Property)]
        public void SetValidMinimumDebounceTime(TimeSpan minimumDebounceTime)
        {
            using var debouncer = new Debouncer
            {
                MinimumDebounceTime = minimumDebounceTime
            };
            Assert.AreEqual(debouncer.MinimumDebounceTime, minimumDebounceTime);
        }

        public static IEnumerable<object[]> InvalidMinimumDebounceTimes
        {
            get
            {
                yield return new object[] { TimeSpan.FromMilliseconds(-1) };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(InvalidMinimumDebounceTimes), DynamicDataSourceType.Property)]
        public void SetInvalidMinimumDebounceTime(TimeSpan minimumDebounceTime)
        {
            using var debouncer = new Debouncer()
            {
                MinimumDebounceTime = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.MinimumDebounceTime = minimumDebounceTime);
            Assert.AreEqual(debouncer.MinimumDebounceTime, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public async Task SingleTriggerDelay()
        {
            using var debouncer = new Debouncer()
            {
                MinimumDebounceTime = TimeSpan.FromMilliseconds(100)
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
