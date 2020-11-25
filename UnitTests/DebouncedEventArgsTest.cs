using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dorssel.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    [TestCategory("Production")]
    [ExcludeFromCodeCoverage]
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
    }
}
