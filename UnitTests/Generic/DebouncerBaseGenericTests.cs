// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace UnitTests.Generic;

[TestClass]
[TestCategory("Production")]
public class DebouncerBaseGenericTests
{
    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(1, 0, 1)]
    [DataRow(long.MaxValue - 1, 0, long.MaxValue - 1)]
    [DataRow(long.MaxValue, 0, long.MaxValue)]
    [DataRow(0, 1, 1)]
    [DataRow(1, 1, 2)]
    [DataRow(long.MaxValue - 1, 1, long.MaxValue)]
    [DataRow(long.MaxValue, 1, long.MaxValue)]
    [DataRow(0, long.MaxValue - 1, long.MaxValue - 1)]
    [DataRow(1, long.MaxValue - 1, long.MaxValue)]
    [DataRow(long.MaxValue - 1, long.MaxValue - 1, long.MaxValue)]
    [DataRow(long.MaxValue, long.MaxValue - 1, long.MaxValue)]
    [DataRow(0, long.MaxValue, long.MaxValue)]
    [DataRow(1, long.MaxValue, long.MaxValue)]
    [DataRow(long.MaxValue - 1, long.MaxValue, long.MaxValue)]
    [DataRow(long.MaxValue, long.MaxValue, long.MaxValue)]
    public void AddWithClamp(long left, long right, long expected)
    {
        Assert.AreEqual(expected, DebouncerBase<DebouncedEventArgs>.AddWithClamp(left, right));
    }
}
