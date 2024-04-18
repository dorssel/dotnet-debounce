// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using Void = Dorssel.Utilities.Void;

namespace UnitTests;

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

    [TestMethod]
    [DynamicData(nameof(ValidCounts))]
    public void ConstructorCountValid(long count)
    {
        var debouncedEventArgs = new DebouncedEventArgs<Void>(count, []);
        Assert.AreEqual(count, debouncedEventArgs.Count);
    }

    [TestMethod]
    [DynamicData(nameof(InvalidCounts))]
    public void ConstructorCountInvalid(long count)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new DebouncedEventArgs<Void>(count, []));
    }

    sealed class DerivedDebouncedEventArgs(long count, bool boundsCheck) : DebouncedEventArgs<Void>(count, boundsCheck, [])
    {
    }

    [TestMethod]
    [DynamicData(nameof(ValidCounts))]
    public void ProtectedConstructorBoundsCheckedValid(long count)
    {
        var debouncedEventArgs = new DerivedDebouncedEventArgs(count, true);
        Assert.AreEqual(count, debouncedEventArgs.Count);
    }

    [TestMethod]
    [DynamicData(nameof(InvalidCounts))]
    public void ProtectedConstructorBoundsCheckedInvalid(long count)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _ = new DerivedDebouncedEventArgs(count, true));
    }

    [TestMethod]
    [DynamicData(nameof(ValidCounts))]
    [DynamicData(nameof(InvalidCounts))]
    public void ProtectedConstructorBoundsUnchecked(long count)
    {
        var debouncedEventArgs = new DerivedDebouncedEventArgs(count, false);
        Assert.AreEqual(count, debouncedEventArgs.Count);
    }
}
