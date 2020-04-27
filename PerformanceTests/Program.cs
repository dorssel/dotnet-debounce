using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dorssel.Utility;

namespace PerformanceTests
{
    static class Program
    {
        const long TriggersPerRound = 1000;

        static void Main()
        {
#if false
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
                    for (int i = 0; i < TriggersPerRound; ++i)
                    {
                        debouncer.Trigger();
                    }
                    triggers += TriggersPerRound;
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
                    for (int i = 0; i < TriggersPerRound; ++i)
                    {
                        debouncer.Trigger();
                    }
                    triggers += TriggersPerRound;
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
                    for (int i = 0; i < TriggersPerRound; ++i)
                    {
                        debouncer.Trigger();
                    }
                    triggers += TriggersPerRound;
                } while (stopwatch.ElapsedMilliseconds < 1000);
                stopwatch.Stop();
                Thread.Sleep(100);
                Console.WriteLine($"Handler speed: {(long)(triggers / stopwatch.Elapsed.TotalSeconds)} triggers/s " +
                    $"@ {(long)(handlers / stopwatch.Elapsed.TotalSeconds)} handlers/s and {triggers - processed} missed");
            }
            {
                using var debouncer = new Debouncer()
                {
                    DebounceInterval = TimeSpan.FromMilliseconds(10)
                };
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
                    for (int i = 0; i < TriggersPerRound; ++i)
                    {
                        debouncer.Trigger();
                    }
                    triggers += TriggersPerRound;
                } while (stopwatch.ElapsedMilliseconds < 1000);
                stopwatch.Stop();
                Thread.Sleep(100);
                Console.WriteLine($"Trigger speed: {(long)(triggers / stopwatch.Elapsed.TotalSeconds)} triggers/s " +
                    $"({handlers} handlers and {triggers - processed} missed)");
            }
#endif
            if (Environment.ProcessorCount < 3)
            {
                Console.WriteLine("Multi-threaded test require at least 3 CPU cores.");
            }
            {
                using var debouncer = new Debouncer()
                {
                    DebounceInterval = TimeSpan.FromMilliseconds(10),
                    DebounceTimeout = TimeSpan.FromMilliseconds(100)
                };
                long handlers = 0;
                long processed = 0;
                debouncer.Debounced += (s, e) =>
                {
                    processed += e.Count;
                    ++handlers;
                };
                // Leave one CPU core for other stuff (including the timer callback thread).
                var tasks = new Task[Environment.ProcessorCount - 1];
                using var startEvent = new ManualResetEventSlim();
                using var stopEvent = new ManualResetEventSlim();
                long triggers = 0;
                var stopwatch = new Stopwatch();
                for (int i = 0; i < tasks.Length; ++i)
                {
                    tasks[i] = Task.Run(() =>
                    {
                        long threadTriggers = 0;
                        startEvent.Wait();
                        do
                        {
                            for (int i = 0; i < TriggersPerRound; ++i)
                            {
                                debouncer.Trigger();
                            }
                            threadTriggers += TriggersPerRound;
                        } while (!stopEvent.IsSet);
                        Interlocked.Add(ref triggers, threadTriggers);
                    });
                }
                // allow CLR to settle before starting the test
                Thread.Sleep(100);
                {
                    // actual test
                    stopwatch.Start();
                    startEvent.Set();
                    Thread.Sleep(1000);
                    stopEvent.Set();
                    stopwatch.Stop();
                    Task.WaitAll(tasks);
                }
                // allow any remaining handlers to be called, so we can check that everything is accounted for
                Thread.Sleep(200);
                Console.WriteLine($"Concurrent trigger speed: {(long)(triggers / stopwatch.Elapsed.TotalSeconds)} triggers/s " +
                    $"({handlers} handlers and {triggers - processed} missed, {debouncer.RescheduleCount} reschedules)");
            }
        }
    }
}
