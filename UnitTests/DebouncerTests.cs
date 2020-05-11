using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Dorssel.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
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
                DebounceWindow = 2 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            debouncer.Dispose();
            Assert.AreEqual(0UL, wrapper.HandlerCount);
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
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
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
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
        }
        #endregion

        #region DebounceWindow
        [TestMethod]
        public void DebounceWindowDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(TimeSpan.Zero, debouncer.DebounceWindow);
        }

        [DataTestMethod]
        [DynamicData(nameof(NonNegativeTimeSpans))]
        public void DebounceWindowValid(TimeSpan DebounceWindow)
        {
            using var debouncer = new Debouncer
            {
                DebounceWindow = ArbitraryNonDefaultTimeSpan
            };
            debouncer.DebounceWindow = DebounceWindow;
            Assert.AreEqual(DebounceWindow, debouncer.DebounceWindow);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void DebounceWindowInvalid(TimeSpan DebounceWindow)
        {
            using var debouncer = new Debouncer()
            {
                DebounceWindow = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.DebounceWindow = DebounceWindow);
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
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceWindow = TimeSpan.Zero);
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
        public void DebounceTimeoutAfterDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceTimeout = TimeSpan.Zero);
        }
        #endregion

        #region EventSpacing
        [TestMethod]
        public void EventSpacingDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(TimeSpan.Zero, debouncer.EventSpacing);
        }

        [DataTestMethod]
        [DynamicData(nameof(NonNegativeTimeSpans))]
        public void EventSpacingValid(TimeSpan eventSpacing)
        {
            using var debouncer = new Debouncer
            {
                EventSpacing = ArbitraryNonDefaultTimeSpan
            };
            debouncer.EventSpacing = eventSpacing;
            Assert.AreEqual(eventSpacing, debouncer.EventSpacing);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void EventSpacingInvalid(TimeSpan eventSpacing)
        {
            using var debouncer = new Debouncer()
            {
                EventSpacing = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.EventSpacing = eventSpacing);
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
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.EventSpacing = TimeSpan.Zero);
        }
        #endregion

        #region HandlerSpacing
        [TestMethod]
        public void HandlerSpacingDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(TimeSpan.Zero, debouncer.HandlerSpacing);
        }

        [DataTestMethod]
        [DynamicData(nameof(NonNegativeTimeSpans))]
        public void HandlerSpacingValid(TimeSpan HandlerSpacing)
        {
            using var debouncer = new Debouncer
            {
                HandlerSpacing = ArbitraryNonDefaultTimeSpan
            };
            debouncer.HandlerSpacing = HandlerSpacing;
            Assert.AreEqual(HandlerSpacing, debouncer.HandlerSpacing);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void HandlerSpacingInvalid(TimeSpan HandlerSpacing)
        {
            using var debouncer = new Debouncer()
            {
                HandlerSpacing = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.HandlerSpacing = HandlerSpacing);
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
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.HandlerSpacing = TimeSpan.Zero);
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
                DebounceWindow = TimeSpan.MaxValue,
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
                DebounceWindow = TimeSpan.MaxValue,
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
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.TimingGranularity = TimeSpan.Zero);
        }
        #endregion

        #region EventHandler
        [TestMethod]
        public void EventHandlerAcceptsEventArgs()
        {
            static void Handler(object? sender, EventArgs eventArgs) { }

            using var debouncer = new Debouncer();
            debouncer.Debounced += Handler;
        }

        [TestMethod]
        public void EventHandlerAcceptsDebouncedEventArgs()
        {
            static void Handler(object? sender, DebouncedEventArgs debouncedEventArgs) { }

            using var debouncer = new Debouncer();
            debouncer.Debounced += Handler;
        }

        [TestMethod]
        public void EventHandlerAcceptsIDebouncedEventArgs()
        {
            static void Handler(object? sender, IDebouncedEventArgs debouncedEventArgs) { }

            using var debouncer = new Debouncer();
            debouncer.Debounced += Handler;
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
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggerSingleDelay()
        {
            using var debouncer = new Debouncer()
            {
                DebounceWindow = 2 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(0UL, wrapper.TriggerCount);
            Assert.AreEqual(0UL, wrapper.HandlerCount);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggersWithTimeout()
        {
            using var debouncer = new Debouncer()
            {
                DebounceWindow = 2 * TimingUnit,
                DebounceTimeout = 4 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            for (var i = 0; i < 6; ++i)
            {
                debouncer.Trigger();
                Thread.Sleep(TimingUnit);
            }
            Assert.AreEqual(1UL, wrapper.HandlerCount);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(6UL, wrapper.TriggerCount);
            Assert.AreEqual(2UL, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggerCoalescence()
        {
            using var debouncer = new Debouncer()
            {
                DebounceWindow = TimingUnit,
                TimingGranularity = TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            for (var i = 0; i < 10; ++i)
            {
                debouncer.Trigger();
            }
            Thread.Sleep(4 * TimingUnit);
            Assert.AreEqual(10UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
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
            Assert.AreEqual(2UL, wrapper.TriggerCount);
            Assert.AreEqual(2UL, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggerDuringHandlerSpacing()
        {
            using var debouncer = new Debouncer()
            {
                HandlerSpacing = 3 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(2UL, wrapper.TriggerCount);
            Assert.AreEqual(2UL, wrapper.HandlerCount);
        }

        [TestMethod]
        public void TriggerDuringEventSpacing()
        {
            using var debouncer = new Debouncer()
            {
                EventSpacing = 3 * TimingUnit
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(2UL, wrapper.TriggerCount);
            Assert.AreEqual(2UL, wrapper.HandlerCount);
        }

        [TestMethod]
        public void CoalesceDuringHandler()
        {
            using var debouncer = new Debouncer()
            {
                DebounceWindow = TimingUnit,
                TimingGranularity = TimingUnit / 10,
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            wrapper.Debounced += (s, e) => Thread.Sleep(2 * TimingUnit);
            debouncer.Trigger();
            Thread.Sleep(2 * TimingUnit);
            debouncer.Trigger();
            debouncer.Trigger();
            Thread.Sleep(5 * TimingUnit);
            Assert.AreEqual(3UL, wrapper.TriggerCount);
            Assert.AreEqual(2UL, wrapper.HandlerCount);
        }
        #endregion

        [TestMethod]
        public void TimingMaximum()
        {
            using var debouncer = new Debouncer()
            {
                DebounceWindow = TimeSpan.MaxValue,
                TimingGranularity = TimeSpan.MaxValue,
            };
            using var wrapper = new VerifyingHandlerWrapper(debouncer);
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(0UL, wrapper.HandlerCount);
            debouncer.TimingGranularity = TimeSpan.Zero;
            debouncer.DebounceWindow = TimeSpan.Zero;
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(1UL, wrapper.TriggerCount);
            Assert.AreEqual(1UL, wrapper.HandlerCount);
        }

        [TestMethod]
        public void BenchmarkDefaults()
        {
            using var debouncer = new Debouncer();
            var benchmark = debouncer.Benchmark;
            Assert.AreEqual(0UL, benchmark.HandlersCalled);
            Assert.AreEqual(0UL, benchmark.TriggersReported);
            Assert.AreEqual(0UL, benchmark.RescheduleCount);
            Assert.AreEqual(0UL, benchmark.TimerChanges);
            Assert.AreEqual(0UL, benchmark.TimerEvents);
        }
    }
}
