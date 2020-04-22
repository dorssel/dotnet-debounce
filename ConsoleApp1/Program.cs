using System;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1
{
    static class Program
    {
        internal static void Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Hello World! Привет мир! Γειά σου Κόσμε!");
            Console.WriteLine(IntPtr.Size);
            Console.WriteLine(Stopwatch.Frequency);
            long start = Stopwatch.GetTimestamp();
            long old_seconds = 0;
            while (true)
            {
                long now = Stopwatch.GetTimestamp();
                long seconds = (long)((double)(now - start) / Stopwatch.Frequency);
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
