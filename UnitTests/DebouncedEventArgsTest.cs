// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace UnitTests;

[TestClass]
[TestCategory("Production")]
sealed class DebouncedEventArgsTests
{
    static readonly IEnumerable<long> ValidCounts = [
        1,
        2,
        int.MaxValue - 1,
        int.MaxValue,
        (long)int.MaxValue + 1,
        uint.MaxValue - 1,
        uint.MaxValue,
        (long)uint.MaxValue + 1,
        long.MaxValue - 1,
        long.MaxValue,
    ];

    static readonly IEnumerable<long> InvalidCounts = [
        0,
        -1,
        long.MinValue + 1,
        long.MinValue
    ];

    [TestMethod]
    [DynamicData(nameof(ValidCounts))]
    public void ConstructorCountValid(long count)
    {
        var debouncedEventArgs = new DebouncedEventArgs(count);
        Assert.AreEqual(count, debouncedEventArgs.Count);
    }

    [TestMethod]
    [DynamicData(nameof(InvalidCounts))]
    public void ConstructorCountInvalid(long count)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new DebouncedEventArgs(count));
    }

    sealed class DerivedDebouncedEventArgs(long count, bool boundsCheck) : DebouncedEventArgs(count, boundsCheck)
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
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new DerivedDebouncedEventArgs(count, true));
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
