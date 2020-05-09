using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dorssel.Utility;

namespace PerformanceTests
{
    sealed class BenchmarkTest : IDisposable
    {
        public BenchmarkTest(Debouncer debouncer)
        {
            Debouncer = debouncer;
            Stopwatch.Start();
        }

        public bool IsFinished { get => CancellationTokenSource.IsCancellationRequested; }

        public void WaitUntilFinished()
        {
            CancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        readonly Debouncer Debouncer;
        readonly Stopwatch Stopwatch = new Stopwatch();
        readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

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
            Debouncer.BackoffInterval = TimeSpan.Zero;
            Debouncer.TimingGranularity = TimeSpan.Zero;
            Debouncer.DebounceInterval = TimeSpan.Zero;
            Stopwatch.Stop();
            // the final handler will be called on some other thread, make sure it has finished
            Thread.Sleep(100);
            var benchmark = Debouncer.Benchmark;
            Console.WriteLine($"   time (ms):         {Stopwatch.ElapsedMilliseconds}");
            Console.WriteLine($"   triggers reported: {benchmark.TriggersReported}");
            Console.WriteLine($"   handlers called:   {benchmark.HandlersCalled}");
            Console.WriteLine($"   reschedules:       {benchmark.RescheduleCount}");
            Console.WriteLine($"   timer changes:     {benchmark.TimerChanges}");
            Console.WriteLine($"   timer events:      {benchmark.TimerEvents}");
            Console.WriteLine();

            CancellationTokenSource.Dispose();
            Debouncer.Dispose();
        }
    }

    static class Program
    {
        static void Main()
        {
            {
                Console.WriteLine("Single-threaded trigger speed (coaslesced)");
                using var debouncer = new Debouncer()
                {
                    DebounceInterval = TimeSpan.MaxValue,
                    TimingGranularity = TimeSpan.MaxValue
                };

                using var test = new BenchmarkTest(debouncer);
                while (!test.IsFinished)
                {
                    test.Trigger1k();
                }
            }
            {
                Console.WriteLine("Multi-threaded trigger speed (coaslesced)");
                using var debouncer = new Debouncer()
                {
                    DebounceInterval = TimeSpan.MaxValue,
                    TimingGranularity = TimeSpan.MaxValue
                };

                using var test = new BenchmarkTest(debouncer);
                var tasks = new Task[Environment.ProcessorCount - 1];
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
            {
                Console.WriteLine("Handler speed (self induced)");
                using var debouncer = new Debouncer();
                void handler(object? s, IDebouncedEventArgs e)
                {
                    debouncer.Trigger();
                }
                debouncer.Debounced += handler;

                using var test = new BenchmarkTest(debouncer);
                debouncer.Trigger();
                test.WaitUntilFinished();
                debouncer.Debounced -= handler;
            }
            {
                // BUGBUG: should be coalescing triggers while handling events!
                Console.WriteLine("Handler speed (single-threaded triggers)");
                using var debouncer = new Debouncer();
                debouncer.Debounced += (s, e) => { };

                using var test = new BenchmarkTest(debouncer);
                while (!test.IsFinished)
                {
                    test.Trigger1k();
                }
            }
            {
                // BUGBUG: should be coalescing triggers while handling events!
                Console.WriteLine("Handler speed (multi-threaded triggers)");
                using var debouncer = new Debouncer();
                debouncer.Debounced += (s, e) => { };

                using var test = new BenchmarkTest(debouncer);
                var tasks = new Task[Environment.ProcessorCount - 1];
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
        }
    }
}
