using System;
using System.Collections.Generic;
using System.Threading;
using Dorssel.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class DebouncerTests
    {
        static readonly TimeSpan TimingUnit = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Some non-default TimeSpan that is also not equal to any of the valid/invalid TimeSpan test values.
        /// </summary>
        static readonly TimeSpan ArbitraryNonDefaultTimeSpan = TimeSpan.FromSeconds(Math.PI);

        public static IEnumerable<object[]> NonNegativeTimeSpans
        {
            get
            {
                yield return new object[] { TimeSpan.MaxValue };
                yield return new object[] { TimeSpan.FromDays(1) };
                yield return new object[] { TimeSpan.FromHours(1) };
                yield return new object[] { TimeSpan.FromMinutes(1) };
                yield return new object[] { TimeSpan.FromSeconds(1) };
                yield return new object[] { TimeSpan.FromMilliseconds(1) };
                yield return new object[] { TimeSpan.FromTicks(1) };
                yield return new object[] { TimeSpan.Zero };
            }
        }

        public static IEnumerable<object[]> InfiniteTimeSpan
        {
            get
            {
                yield return new object[] { Timeout.InfiniteTimeSpan };
            }
        }

        public static IEnumerable<object[]> NegativeTimeSpans
        {
            get
            {
                // NOTE: FromMilliseconds(-1) == InfiniteTimeSpan, a  magic value
                yield return new object[] { TimeSpan.FromTicks(-1) };
                yield return new object[] { TimeSpan.FromMilliseconds(-2) };
                yield return new object[] { TimeSpan.FromSeconds(-1) };
                yield return new object[] { TimeSpan.FromMinutes(-1) };
                yield return new object[] { TimeSpan.FromHours(-1) };
                yield return new object[] { TimeSpan.FromDays(-1) };
                yield return new object[] { TimeSpan.MinValue };
            }
        }

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

        [TestMethod]
        public void DisposeDuringTimer()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = 2 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            debouncer.Dispose();
            Assert.AreEqual(0, wrapper.HandlerCount);
        }

        [TestMethod]
        public void DisposeDuringHandler()
        {
            var debouncer = new Debouncer();
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            using var done = new ManualResetEventSlim();
            wrapper.Debounced += (s, e) =>
            {
                Thread.Sleep(2 * TimingUnit);
                done.Set();
            };
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            debouncer.Dispose();
            Assert.IsTrue(done.Wait(2 * TimingUnit));
            Assert.AreEqual(1, wrapper.TriggerCount);
            Assert.AreEqual(1, wrapper.HandlerCount);
        }

        [TestMethod]
        public void DisposeFromHandler()
        {
            using var debouncer = new Debouncer();
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            using var done = new ManualResetEventSlim();
            wrapper.Debounced += (s, e) =>
            {
                debouncer.Dispose();
                done.Set();
            };
            debouncer.Trigger();
            Assert.IsTrue(done.Wait(TimingUnit));
            Assert.AreEqual(1, wrapper.TriggerCount);
            Assert.AreEqual(1, wrapper.HandlerCount);
        }
        #endregion

        #region DebounceInterval
        [TestMethod]
        public void DebounceIntervalDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(TimeSpan.Zero, debouncer.DebounceInterval);
        }

        [DataTestMethod]
        [DynamicData(nameof(NonNegativeTimeSpans))]
        public void DebounceIntervalValid(TimeSpan debounceInterval)
        {
            using var debouncer = new Debouncer
            {
                DebounceInterval = ArbitraryNonDefaultTimeSpan
            };
            debouncer.DebounceInterval = debounceInterval;
            Assert.AreEqual(debounceInterval, debouncer.DebounceInterval);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void DebounceIntervalInvalid(TimeSpan debounceInterval)
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.DebounceInterval = debounceInterval);
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceInterval);
        }

        [TestMethod]
        public void DebounceIntervalUnchanged()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceInterval);
            debouncer.DebounceInterval = TimeSpan.FromMilliseconds(1);
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.DebounceInterval);
        }

        [TestMethod]
        public void DebounceIntervalEqualsDebounceTimeout()
        {
            using var debouncer = new Debouncer()
            {
                DebounceTimeout = TimeSpan.FromSeconds(1)
            };
            debouncer.DebounceInterval = TimeSpan.FromSeconds(1);
        }

        [TestMethod]
        public void DebounceIntervalExceedsDebounceTimeout()
        {
            using var debouncer = new Debouncer()
            {
                DebounceTimeout = TimeSpan.FromSeconds(1)
            };
            Assert.ThrowsException<ArgumentException>(() => debouncer.DebounceInterval = TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        public void DebounceIntervalEqualsTimingGranularity()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromSeconds(2),
                TimingGranularity = TimeSpan.FromSeconds(1)
            };
            debouncer.DebounceInterval = TimeSpan.FromSeconds(1);
        }

        [TestMethod]
        public void DebounceIntervalLessThanTimingGranularity()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromSeconds(2),
                TimingGranularity = TimeSpan.FromSeconds(2)
            };
            Assert.ThrowsException<ArgumentException>(() => debouncer.DebounceInterval = TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void DebounceIntervalAfterDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceInterval = TimeSpan.Zero);
        }
        #endregion

        #region DebounceTimeout
        [TestMethod]
        public void DebounceTimeoutDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(Timeout.InfiniteTimeSpan, debouncer.DebounceTimeout);
        }

        [DataTestMethod]
        [DynamicData(nameof(NonNegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void DebounceTimeoutValid(TimeSpan debounceTimeout)
        {
            using var debouncer = new Debouncer
            {
                DebounceTimeout = ArbitraryNonDefaultTimeSpan
            };
            debouncer.DebounceTimeout = debounceTimeout;
            Assert.AreEqual(debounceTimeout, debouncer.DebounceTimeout);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
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
        public void DebounceTimeoutLessThanDebounceInterval()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromSeconds(2)
            };
            Assert.ThrowsException<ArgumentException>(() => debouncer.DebounceTimeout = TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void DebounceTimeoutAfterDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceTimeout = TimeSpan.Zero);
        }
        #endregion

        #region Backoff
        [TestMethod]
        public void BackoffIntervalDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(TimeSpan.Zero, debouncer.BackoffInterval);
        }

        [DataTestMethod]
        [DynamicData(nameof(NonNegativeTimeSpans))]
        public void BackoffIntervalValid(TimeSpan backoffInterval)
        {
            using var debouncer = new Debouncer
            {
                BackoffInterval = ArbitraryNonDefaultTimeSpan
            };
            debouncer.BackoffInterval = backoffInterval;
            Assert.AreEqual(backoffInterval, debouncer.BackoffInterval);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void BackoffIntervalInvalid(TimeSpan backoffInterval)
        {
            using var debouncer = new Debouncer()
            {
                BackoffInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.BackoffInterval = backoffInterval);
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.BackoffInterval);
        }

        [TestMethod]
        public void BackoffIntervalUnchanged()
        {
            using var debouncer = new Debouncer()
            {
                BackoffInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.BackoffInterval);
            debouncer.BackoffInterval = TimeSpan.FromMilliseconds(1);
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), debouncer.BackoffInterval);
        }

        [TestMethod]
        public void BackoffIntervalAfterDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.BackoffInterval = TimeSpan.Zero);
        }
        #endregion

        #region TimingGranularity
        [TestMethod]
        public void TimingGranularityDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(TimeSpan.Zero, debouncer.TimingGranularity);
        }

        [DataTestMethod]
        [DynamicData(nameof(NonNegativeTimeSpans))]
        public void TimingGranularityValid(TimeSpan timingGranularity)
        {
            using var debouncer = new Debouncer
            {
                DebounceInterval = TimeSpan.MaxValue,
                TimingGranularity = ArbitraryNonDefaultTimeSpan
            };
            debouncer.TimingGranularity = timingGranularity;
            Assert.AreEqual(timingGranularity, debouncer.TimingGranularity);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void TimingGranularityInvalid(TimeSpan timingGranularity)
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.MaxValue,
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
                DebounceInterval = TimeSpan.MaxValue,
                TimingGranularity = TimeSpan.FromMilliseconds(2)
            };
            Assert.AreEqual(TimeSpan.FromMilliseconds(2), debouncer.TimingGranularity);
            debouncer.TimingGranularity = TimeSpan.FromMilliseconds(2);
            Assert.AreEqual(TimeSpan.FromMilliseconds(2), debouncer.TimingGranularity);
        }

        [TestMethod]
        public void TimingGranularityEqualsDebounceInterval()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromSeconds(1)
            };
            debouncer.TimingGranularity = TimeSpan.FromSeconds(1);
        }

        [TestMethod]
        public void TimingGranularityExceedsDebounceInterval()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromSeconds(1)
            };
            Assert.ThrowsException<ArgumentException>(() => debouncer.TimingGranularity = TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        public void TimingGranularityAfterDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.TimingGranularity = TimeSpan.Zero);
        }
        #endregion

        #region Trigger
        [TestMethod]
        public void TriggerWithoutHandlers()
        {
            {
                using var debouncer = new Debouncer();
                debouncer.Trigger();
            }
            Thread.Sleep(TimingUnit);
        }

        [TestMethod]
        public void TriggerAfterDispose()
        {
            using var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.Trigger());
        }

        [TestMethod]
        public void TriggerSingle()
        {
            using var debouncer = new Debouncer();
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1, wrapper.TriggerCount);
            Assert.AreEqual(1, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggerSingleDelay()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = 2 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(0, wrapper.TriggerCount);
            Assert.AreEqual(0, wrapper.HandlerCount);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(1, wrapper.TriggerCount);
            Assert.AreEqual(1, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggersWithTimeout()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = 2 * TimingUnit,
                DebounceTimeout = 4 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            for (var i = 0; i < 6; ++i)
            {
                debouncer.Trigger();
                Thread.Sleep(TimingUnit);
            }
            Assert.AreEqual(1, wrapper.HandlerCount);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(6, wrapper.TriggerCount);
            Assert.AreEqual(2, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggerCoalescence()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = 2 * TimingUnit,
                TimingGranularity = TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            for (var i = 0; i < 10; ++i)
            {
                debouncer.Trigger();
            }
            Thread.Sleep(4 * TimingUnit);
            Assert.AreEqual(10, wrapper.TriggerCount);
            Assert.AreEqual(1, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggerFromHandler()
        {
            using var debouncer = new Debouncer();
            using var wrapper = new VerifyingHandlerWrapper(debouncer);

            wrapper.Debounced += (s, e) =>
            {
                if (wrapper.HandlerCount == 1)
                {
                    debouncer.Trigger();
                }
            };
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(2, wrapper.TriggerCount);
            Assert.AreEqual(2, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggerDuringBackoff()
        {
            using var debouncer = new Debouncer()
            {
                BackoffInterval = 3 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1, wrapper.TriggerCount);
            Assert.AreEqual(1, wrapper.HandlerCount);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1, wrapper.TriggerCount);
            Assert.AreEqual(1, wrapper.HandlerCount);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(2, wrapper.TriggerCount);
            Assert.AreEqual(2, wrapper.HandlerCount);
        }

        [TestMethod]
        public void CoalesceDuringHandler()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimingUnit,
                TimingGranularity = TimingUnit / 10,
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            wrapper.Debounced += (s, e) => Thread.Sleep(2 * TimingUnit);
            debouncer.Trigger();
            Thread.Sleep(2 * TimingUnit);
            debouncer.Trigger();
            debouncer.Trigger();
            Thread.Sleep(5 * TimingUnit);
            Assert.AreEqual(3, wrapper.TriggerCount);
            Assert.AreEqual(2, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TimingMaximum()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.MaxValue,
                TimingGranularity = TimeSpan.MaxValue,
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(0, wrapper.HandlerCount);
            debouncer.TimingGranularity = TimeSpan.Zero;
            debouncer.DebounceInterval = TimeSpan.Zero;
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1, wrapper.TriggerCount);
            Assert.AreEqual(1, wrapper.HandlerCount);
        }
        #endregion
    }
}
