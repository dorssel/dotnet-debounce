using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ConsoleApp1
{
    [ExcludeFromCodeCoverage]
    static class Program
    {
        internal static void Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Hello World! Привет мир! Γειά σου Κόσμε!");
            Console.WriteLine(IntPtr.Size);
            Console.WriteLine(Stopwatch.Frequency);
            var start = Stopwatch.GetTimestamp();
            var old_seconds = 0L;
            while (true)
            {
                var now = Stopwatch.GetTimestamp();
                var seconds = (long)((double)(now - start) / Stopwatch.Frequency);
                if (old_seconds != seconds)
                {
                    Console.WriteLine($"{seconds}: {now}");
                    old_seconds = seconds;
                }
                if (seconds >= 5)
                {
                    break;
                }
            }
        }
    }
}
