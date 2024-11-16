// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Dorssel.Utilities;

namespace PerformanceTests;

static class Program
{
    sealed class BenchmarkTest : IDisposable
    {
        public BenchmarkTest(Debouncer debouncer)
        {
            Debouncer = debouncer;
            Stopwatch.Start();
        }

        public bool IsFinished => CancellationTokenSource.IsCancellationRequested;

        public void WaitUntilFinished()
        {
            _ = CancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        readonly Debouncer Debouncer;
        readonly Stopwatch Stopwatch = new();
        readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(1));

        public void Trigger1k()
        {
            for (var i = 0; i < 100; ++i)
            {
                Debouncer.Trigger();
                Debouncer.Trigger();
                Debouncer.Trigger();
                Debouncer.Trigger();
                Debouncer.Trigger();
                Debouncer.Trigger();
                Debouncer.Trigger();
                Debouncer.Trigger();
                Debouncer.Trigger();
                Debouncer.Trigger();
            }
        }

        public void Dispose()
        {
            // make sure any remaining handlers are called immediately
            Debouncer.HandlerSpacing = TimeSpan.Zero;
            Debouncer.TimingGranularity = TimeSpan.Zero;
            Debouncer.DebounceWindow = TimeSpan.Zero;
            Stopwatch.Stop();
            // the final handler will be called on some other thread, make sure it has finished
            Thread.Sleep(100);
            // report
            var benchmark = Debouncer.Benchmark;
            Console.WriteLine($"   time (ms):         {Stopwatch.ElapsedMilliseconds}");
            Console.WriteLine($"   triggers reported: {benchmark.TriggersReported}");
            Console.WriteLine($"   handlers called:   {benchmark.HandlersCalled}");
            Console.WriteLine($"   reschedules:       {benchmark.RescheduleCount}");
            Console.WriteLine($"   timer changes:     {benchmark.TimerChanges}");
            Console.WriteLine($"   timer events:      {benchmark.TimerEvents}");
            Console.WriteLine();

            // dispose
            CancellationTokenSource.Dispose();
            Debouncer.Dispose();
        }
    }

    static void TriggerTest(uint taskCount, bool coalesce)
    {
        using var debouncer = new Debouncer()
        {
            DebounceWindow = coalesce ? TimeSpan.MaxValue : TimeSpan.Zero,
            // with a fixed DebounceWindow of 0, this will only coalesce during the handler
            TimingGranularity = TimeSpan.MaxValue
        };

        using var test = new BenchmarkTest(debouncer);
        var tasks = new Task[taskCount];
        foreach (ref var task in tasks.AsSpan())
        {
            task = Task.Run(() =>
            {
                while (!test.IsFinished)
                {
                    test.Trigger1k();
                }
            });
        }
        Task.WaitAll(tasks);
    }

    // we're testing the performance of the Debouncer, not of the CLR thread scheduler,
    // so leave one processor free.
    static readonly uint MaxTasks = (uint)Environment.ProcessorCount - 1;

    static void Main()
    {
        Console.WriteLine("Single-threaded trigger speed");
        TriggerTest(1, false);

        if (MaxTasks >= 2)
        {
            Console.WriteLine("Multi-threaded (2) trigger speed");
            TriggerTest(2, false);
        }

        if (MaxTasks > 2)
        {
            Console.WriteLine($"Multi-threaded ({MaxTasks}) trigger speed");
            TriggerTest(MaxTasks, false);
        }

        Console.WriteLine("Single-threaded trigger speed (coalesced)");
        TriggerTest(1, true);

        if (MaxTasks >= 2)
        {
            Console.WriteLine("Multi-threaded (2) trigger speed (coalesced)");
            TriggerTest(2, true);
        }

        if (MaxTasks > 2)
        {
            Console.WriteLine($"Multi-threaded ({MaxTasks}) trigger speed (coalesced)");
            TriggerTest(MaxTasks, true);
        }

        {
            Console.WriteLine("Handler speed");
            using var debouncer = new Debouncer();
            void handler(object? s, DebouncedEventArgs e)
            {
                // each handler triggers the next
                debouncer.Trigger();
            }
            debouncer.Debounced += handler;

            using var test = new BenchmarkTest(debouncer);
            // start the chain
            debouncer.Trigger();
            test.WaitUntilFinished();
            debouncer.Debounced -= handler;
        }

        {
            Console.WriteLine("Timer speed");
            using var debouncer = new Debouncer()
            {
                // smallest value > 0
                DebounceWindow = TimeSpan.FromTicks(1),
                TimingGranularity = TimeSpan.FromTicks(1)
            };
            void handler(object? s, DebouncedEventArgs e)
            {
                // each handler triggers the next
                debouncer.Trigger();
            }
            debouncer.Debounced += handler;

            using var test = new BenchmarkTest(debouncer);
            // start the chain
            debouncer.Trigger();
            test.WaitUntilFinished();
            debouncer.Debounced -= handler;
        }
    }
}
