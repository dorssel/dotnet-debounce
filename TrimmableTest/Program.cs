// SPDX-FileCopyrightText: 2022 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dorssel.Utilities;

[assembly: CLSCompliant(true)]
[assembly: ExcludeFromCodeCoverage]

namespace TrimmableTest
{
    static class Program
    {
        static int Main()
        {
            using var debouncer = new Debouncer();

            if (debouncer.GetType().Assembly.GetTypes().Select(t => t.Name).Contains("IDebounce"))
            {
                Console.Error.WriteLine("error: Not trimmed. Assembly still contains IDebounce.");
                return 1;
            }
            else
            {
                Console.Out.WriteLine("Trimming successful. Assembly no longer contains IDebounce.");
                return 0;
            }
        }
    }
}
