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
            {
                using var debouncer = new Debouncer()
                {
                    DebounceInterval = TimeSpan.FromMilliseconds(10)
                };
                long handlers = 0;
                long processed = 0;
                debouncer.Debounced += (s, e) =>
                {
                    processed += e.Count;
                    ++handlers;
                };
                var threads = new Thread[Environment.ProcessorCount];
                var triggers = new long[threads.Length];
                var triggersPerSecond = new long[threads.Length];
                for (int i = 0; i < threads.Length; ++i) {
                    threads[i] = new Thread((data) =>
                    {
                        int index = (int)data!;
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        do
                        {
                            for (int j = 0; j < TriggersPerRound; ++j)
                            {
                                debouncer.Trigger();
                            }
                            triggers[index] += TriggersPerRound;
                        } while (stopwatch.ElapsedMilliseconds < 1000);
                        stopwatch.Stop();
                        triggersPerSecond[index] = (long)(triggers[index] / stopwatch.Elapsed.TotalSeconds);
                        Thread.Sleep(100);
                        Console.WriteLine($"Thread trigger speed: {triggersPerSecond[index]} triggers/s");
                    });
                }
                for (int i = 0; i < threads.Length; ++i)
                {
                    threads[i].Start(i);
                }
                foreach (var thread in threads) {
                    thread.Join();
                }
                Console.WriteLine($"Multithreaded trigger speed: {triggersPerSecond.Sum()} triggers/s " +
                    $"({handlers} handlers and {triggers.Sum() - processed} missed)");
            }
            {
                using var debouncer = new Debouncer()
                {
                    DebounceInterval = TimeSpan.FromMilliseconds(10)
                };
                long handlers = 0;
                long processed = 0;
                debouncer.Debounced += (s, e) =>
                {
                    processed += e.Count;
                    ++handlers;
                };
                var tasks = new Task[Environment.ProcessorCount];
                var triggers = new long[tasks.Length];
                var triggersPerSecond = new long[tasks.Length];
                for (int i = 0; i < tasks.Length; ++i)
                {
                    int index = i;
                    tasks[i] = Task.Run(() =>
                    {
                        long threadTriggers = 0;
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        do
                        {
                            for (int j = 0; j < TriggersPerRound; ++j)
                            {
                                debouncer.Trigger();
                            }
                            threadTriggers += TriggersPerRound;
                        } while (stopwatch.ElapsedMilliseconds < 1000);
                        stopwatch.Stop();
                        triggers[index] = threadTriggers;
                        triggersPerSecond[index] = (long)(threadTriggers / stopwatch.Elapsed.TotalSeconds);
                        Thread.Sleep(100);
                        Console.WriteLine($"Task trigger speed: {triggersPerSecond[index]} triggers/s");
                    });
                }
                Task.WaitAll(tasks);
                Console.WriteLine($"Concurrent trigger speed: {triggersPerSecond.Sum()} triggers/s " +
                    $"({handlers} handlers and {triggers.Sum() - processed} missed)");
            }
        }
    }
}
