using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace UnitTests
{
    static class TimeSpanData
    {
        /// <summary>
        /// Some non-default TimeSpan that is also not equal to any of the valid/invalid TimeSpan test values.
        /// </summary>
        public static readonly TimeSpan ArbitraryNonDefault = TimeSpan.FromSeconds(Math.PI);

        static readonly TimeSpan[] _NonNegative = new TimeSpan[]
        {
            TimeSpan.MaxValue,
            TimeSpan.FromDays(1),
            TimeSpan.FromHours(1),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromTicks(1),
            TimeSpan.Zero
        };

        static readonly TimeSpan[] _Negative = new TimeSpan[]
        {
            TimeSpan.FromTicks(-1),
            // NOTE: FromMilliseconds(-1) == Timeout.InfiniteTimeSpan, a  magic value
            TimeSpan.FromMilliseconds(-2),
            TimeSpan.FromSeconds(-1),
            TimeSpan.FromMinutes(-1),
            TimeSpan.FromHours(-1),
            TimeSpan.FromDays(-1),
            TimeSpan.MinValue
        };

        public static IEnumerable<object[]> NonNegative
        {
            get => from value in _NonNegative select new object[] { value };
        }

        public static IEnumerable<object[]> Negative
        {
            get => from value in _Negative select new object[] { value };
        }

        public static IEnumerable<object[]> Infinite
        {
            get => new object[][] { new object[] { Timeout.InfiniteTimeSpan } };
        }
    }
}
