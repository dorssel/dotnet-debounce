// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Dorssel.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    [TestCategory("Production")]
    public class DebouncedEventArgsTests
    {
        public static IEnumerable<object[]> ValidCounts
        {
            get
            {
                yield return new object[] { 1L };
                yield return new object[] { 2L };
                yield return new object[] { long.MaxValue - 1 };
                yield return new object[] { long.MaxValue };
            }
        }

        public static IEnumerable<object[]> InvalidCounts
        {
            get
            {
                yield return new object[] { 0L };
                yield return new object[] { -1L };
                yield return new object[] { long.MinValue + 1 };
                yield return new object[] { long.MinValue };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ValidCounts))]
        public void ConstructorCountValid(long count)
        {
            var debouncedEventArgs = new DebouncedEventArgs(count);
            Assert.AreEqual(count, debouncedEventArgs.Count);
        }

        [DataTestMethod]
        [DynamicData(nameof(InvalidCounts))]
        public void ConstructorCountInvalid(long count)
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new DebouncedEventArgs(count));
        }

        class DerivedDebouncedEventArgs : DebouncedEventArgs
        {
            public DerivedDebouncedEventArgs(long count, bool boundsCheck)
                : base(count, boundsCheck)
            { }
        }

        [DataTestMethod]
        [DynamicData(nameof(ValidCounts))]
        public void ProtectedConstructorBoundsCheckedValid(long count)
        {
            var debouncedEventArgs = new DerivedDebouncedEventArgs(count, true);
            Assert.AreEqual(count, debouncedEventArgs.Count);
        }

        [DataTestMethod]
        [DynamicData(nameof(InvalidCounts))]
        public void ProtectedConstructorBoundsCheckedInvalid(long count)
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new DerivedDebouncedEventArgs(count, true));
        }

        [DataTestMethod]
        [DynamicData(nameof(ValidCounts))]
        [DynamicData(nameof(InvalidCounts))]
        public void ProtectedConstructorBoundsUnchecked(long count)
        {
            var debouncedEventArgs = new DerivedDebouncedEventArgs(count, false);
            Assert.AreEqual(count, debouncedEventArgs.Count);
        }
    }
}
