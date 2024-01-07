<!--
SPDX-FileCopyrightText: 2021 Frans van Dorsselaer

SPDX-License-Identifier: MIT
-->

# .NET event debouncer

[![Build](https://github.com/dorssel/dotnet-debounce/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dorssel/dotnet-debounce/actions/workflows/dotnet.yml)
[![Lint](https://github.com/dorssel/dotnet-debounce/actions/workflows/lint.yml/badge.svg)](https://github.com/dorssel/dotnet-debounce/actions/workflows/lint.yml)
[![codecov](https://codecov.io/gh/dorssel/dotnet-debounce/branch/master/graph/badge.svg?token=L0QI0AZRJI)](https://codecov.io/gh/dorssel/dotnet-debounce)

This library exposes a single object: an event debouncer. It can be used to "filter" or "buffer" multiple incoming events into one.
Common uses are:

- Throttling how often the event handler is called.
- Coalescing/debounce multiple events into one; the final event includes to total number of events received.
- Serializing the event handler, such that it is not called re-entrant even if events are fired from multiple concurrent sources.
- Spacing event handler calls, to give the CPU / disk / network some breathing room if events are arriving continuously.

## Examples

### auto-save

You want auto-save for your changing documents. Every key press alters the document, but not every key press should immediately lead
to saving. Instead, the debouncer only fires when no new events are coming in within a configurable time window
(such as when the user stops typing for 10 seconds). But simultaneously,   the event should not be held off forever
(such as when the user types continously). Therefore, the debouncer has a configurable maximum
timeout after which the save event is no longer held back (for example, you want to auto-save no later than 5 minutes after the first
change).

### screen updates

You want your screen to reflect any changes in the underlying data (for example, via `INotifyPropertyChanged`). But if changes are
happening a million times a second, you want the screen to only update a maximum of 50 times per second instead of claiming the CPU entirely.
You configure the debounce window to 5 ms, such that any changes are displayed promptly (while holding off any updates if new event arrive
within 5 ms of the previous event); and the timeout and handler spacing to 20 ms,
such that the screen is updated 50 times a second if needed.

### server-side pushes

Your website server wants to push any update of internal state to the client, but always at least 100 ms apart from any previous
push. You could configure the debounce window and spacing to 100 ms, and the timeout to 1 s. If changes are continuously arriving
(spaced less than 100 ms, within the debounce window), then the push happens every second (the timeout value). But if changes
are sporadic, then they will be pushed after the initial timeout window of 100 ms for "snappiness". Finally, the debouncer will wait at least 100 ms
(the spacer value) after the return of the previous handler, such that the network IO in the handler itself is given a bit
of rest every time.

## Performance

The library was written with performance in mind. The `Trigger` function (to indicate that a source event has arrived) can easily handle
tens of millions of calls per second, coming in from mutliple threads.

Once configured, the debouncer works without object allocation while debouncing.

## Versatility

The library has two targets:

- `netstandard2.0` (which works on all modern .NET versions)
- `netstandard1.2` (for backward compatibility with older .NET Framework and PowerShell versions).

The library is compiled for `AnyCPU`.

The library has no dependencies.

The library uses strong name signing to allow being consumed by other strongly named assemblies.

The public API uses the interface design patern, which allows 100% code coverage testing with mocking (even for edge cases that will
never occur in real life).

The binaries are signed with Authenticode.

The `nuget` package ships with SourceLink and IntelliSense documentation.

## Code Examples

The repository contains two examples:

- A Blazor server-side push example that demonstrates the performance of the library.

- An example that demonstrates how you can get 100% code coverage for classes that use the debouncer.
