// SPDX-FileCopyrightText: 2024 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace UnitTests;

static class CollectionAssertExtentions
{
    public static void AreEqual<TData>(this CollectionAssert customAssert, IEnumerable<TData> expected, IEnumerable<TData> actual)
    {
        _ = customAssert;
        Assert.IsTrue(actual.SequenceEqual(expected), $"Was: [{string.Join(",", actual)}]");
    }
}
