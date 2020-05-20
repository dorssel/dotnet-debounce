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
                yield return new object[] { 1UL };
                yield return new object[] { 2UL };
                yield return new object[] { ulong.MaxValue - 1 };
                yield return new object[] { ulong.MaxValue };
            }
        }

        public static IEnumerable<object[]> InvalidCounts
        {
            get
            {
                yield return new object[] { 0UL };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ValidCounts))]
        public void ConstructorCountValid(ulong count)
        {
            var debouncedEventArgs = new DebouncedEventArgs(count);
            Assert.AreEqual(count, debouncedEventArgs.Count);
        }

        [DataTestMethod]
        [DynamicData(nameof(InvalidCounts))]
        public void ConstructorCountInvalid(ulong count)
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new DebouncedEventArgs(count));
        }
    }
}
