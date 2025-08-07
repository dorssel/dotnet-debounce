// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

using System.Collections;

namespace UnitTests.Generic;

[TestClass]
[TestCategory("Production")]
sealed class DebouncedEventArgsGenericTests
{
    sealed class MockEnumerator(MockReadOnlyList List) : IEnumerator<int>
    {
        int Pos = -1;

        public int Current => List[Pos];

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            if (Pos >= List.Count || (Pos == -1 && List.Count == 0))
            {
                return false;
            }
            Pos++;
            return true;
        }

        public void Reset()
        {
            Pos = -1;
        }
    }

    sealed class MockReadOnlyList(int _Count) : IReadOnlyList<int>
    {
        public int this[int index] => index < 0 || index >= Count ? throw new ArgumentOutOfRangeException(nameof(index)) : index;

        public int Count => _Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MockEnumerator(this);
        }

        public IEnumerator<int> GetEnumerator()
        {
            return new MockEnumerator(this);
        }
    }

    public static IEnumerable<object[]> ValidCounts
    {
        get
        {
            yield return new object[] { 1L, new MockReadOnlyList(0) };
            yield return new object[] { 1L, new MockReadOnlyList(1) };
            yield return new object[] { 2L, new MockReadOnlyList(0) };
            yield return new object[] { 2L, new MockReadOnlyList(1) };
            yield return new object[] { 2L, new MockReadOnlyList(2) };
            yield return new object[] { int.MaxValue - 1, new MockReadOnlyList(0) };
            yield return new object[] { int.MaxValue - 1, new MockReadOnlyList(1) };
            yield return new object[] { int.MaxValue - 1, new MockReadOnlyList(int.MaxValue - 2) };
            yield return new object[] { int.MaxValue - 1, new MockReadOnlyList(int.MaxValue - 1) };
            yield return new object[] { int.MaxValue, new MockReadOnlyList(0) };
            yield return new object[] { int.MaxValue, new MockReadOnlyList(1) };
            yield return new object[] { int.MaxValue, new MockReadOnlyList(int.MaxValue - 1) };
            yield return new object[] { int.MaxValue, new MockReadOnlyList(int.MaxValue) };
        }
    }

    public static IEnumerable<object[]> InvalidCounts
    {
        get
        {
            yield return new object[] { 0L, new MockReadOnlyList(0) };
            yield return new object[] { 0L, new MockReadOnlyList(1) };
            yield return new object[] { 1L, new MockReadOnlyList(2) };
            yield return new object[] { 2L, new MockReadOnlyList(3) };
            yield return new object[] { (long)(int.MaxValue - 1), new MockReadOnlyList(int.MaxValue) };
        }
    }

    [TestMethod]
    [DynamicData(nameof(ValidCounts))]
    public void ConstructorCountValid(long count, IReadOnlyList<int> triggerData)
    {
        var debouncedEventArgs = new DebouncedEventArgs<int>(count, triggerData);
        Assert.AreEqual(count, debouncedEventArgs.Count);
        Assert.AreSame(triggerData, debouncedEventArgs.TriggerData);
    }

    [TestMethod]
    [DynamicData(nameof(InvalidCounts))]
    public void ConstructorCountInvalid(long count, IReadOnlyList<int> triggerData)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DebouncedEventArgs<int>(count, triggerData);
        });
    }

    [TestMethod]
    public void ConstructorNullThrows()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DebouncedEventArgs<int>(1, null!);
        });
    }

    sealed class DerivedDebouncedEventArgs<TData>(long count, IReadOnlyList<TData> triggerData, bool boundsCheck)
        : DebouncedEventArgs<TData>(count, triggerData, boundsCheck)
    {
    }

    [TestMethod]
    [DynamicData(nameof(ValidCounts))]
    public void ProtectedConstructorBoundsCheckedValid(long count, IReadOnlyList<int> triggerData)
    {
        var debouncedEventArgs = new DerivedDebouncedEventArgs<int>(count, triggerData, true);
        Assert.AreEqual(count, debouncedEventArgs.Count);
        Assert.AreSame(triggerData, debouncedEventArgs.TriggerData);
    }

    [TestMethod]
    [DynamicData(nameof(InvalidCounts))]
    public void ProtectedConstructorBoundsCheckedInvalid(long count, IReadOnlyList<int> triggerData)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DerivedDebouncedEventArgs<int>(count, triggerData, true);
        });
    }

    [TestMethod]
    [DynamicData(nameof(ValidCounts))]
    [DynamicData(nameof(InvalidCounts))]
    public void ProtectedConstructorBoundsUnchecked(long count, IReadOnlyList<int> triggerData)
    {
        var debouncedEventArgs = new DerivedDebouncedEventArgs<int>(count, triggerData, false);
        Assert.AreEqual(count, debouncedEventArgs.Count);
        Assert.AreSame(triggerData, debouncedEventArgs.TriggerData);
    }
}
