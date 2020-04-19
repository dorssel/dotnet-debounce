using System;
using System.Diagnostics;
using System.Threading;
using Dorssel.Utility;

namespace PerformanceTests
{
    static class Program
    {
        static void Main()
        {
            {
                using var debouncer = new Debouncer()
                {
                    DebounceInterval = TimeSpan.FromMinutes(1)
                };
                var stopwatch = new Stopwatch();
                long triggers = 0;
                stopwatch.Start();
                do
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        debouncer.Trigger();
                        ++triggers;
                    }
                } while (stopwatch.ElapsedMilliseconds < 1000);
                stopwatch.Stop();
                Console.WriteLine($"Coalescence speed: {(long)(triggers / stopwatch.Elapsed.TotalSeconds)} triggers/s");
            }
            {
                using var debouncer = new Debouncer();
                var stopwatch = new Stopwatch();
                long triggers = 0;
                stopwatch.Start();
                do
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        debouncer.Trigger();
                        ++triggers;
                    }
                } while (stopwatch.ElapsedMilliseconds < 1000);
                stopwatch.Stop();
                Console.WriteLine($"Serialization speed: {(long)(triggers / stopwatch.Elapsed.TotalSeconds)} triggers/s");
            }
            {
                using var debouncer = new Debouncer();
                long triggers = 0;
                long handlers = 0;
                long processed = 0;
                debouncer.Debounced += (s, e) =>
                {
                    processed += e.Count;
                    ++handlers;
                };
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                do
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        debouncer.Trigger();
                        ++triggers;
                    }
                } while (stopwatch.ElapsedMilliseconds < 1000);
                stopwatch.Stop();
                Thread.Sleep(100);
                Console.WriteLine($"Handler speed: {(long)(triggers / stopwatch.Elapsed.TotalSeconds)} triggers/s " +
                    $"@ {(long)(handlers / stopwatch.Elapsed.TotalSeconds)} handlers/s and {triggers - processed} missed");
            }
        }
    }
}
