// SPDX-FileCopyrightText: 2026 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace FuzzingTests;

[TestClass]
sealed class Debounce_Tests(TestContext TestContext)
{
    [TestMethod]
    public void VariableTimeout()
    {
        var timeProvider = new FakeTimeProvider();
        using var debouncer = new Debouncer(timeProvider);

        Prop.ForAll<uint>(microSeconds =>
        {
            debouncer.DebounceTimeout = TimeSpan.FromMicroseconds((long)microSeconds + 1);
            debouncer.Trigger();
            timeProvider.Advance(TimeSpan.FromMicroseconds(microSeconds));
            debouncer.Trigger();
            timeProvider.Advance(TimeSpan.FromMicroseconds(2));
            debouncer.CurrentEventHandlersTask.Wait(TestContext.CancellationToken);
            return debouncer.Reset() == 0;
        }).QuickCheckThrowOnFailure();
    }
}
