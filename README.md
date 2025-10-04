<!--
SPDX-FileCopyrightText: 2021 Frans van Dorsselaer

SPDX-License-Identifier: MIT
-->

# .NET event debouncer

[![Build](https://github.com/dorssel/dotnet-debounce/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/dorssel/dotnet-debounce/actions?query=workflow%3ABuild+branch%3Amaster)
[![CodeQL](https://github.com/dorssel/dotnet-debounce/actions/workflows/codeql.yml/badge.svg?branch=master)](https://github.com/dorssel/dotnet-debounce/actions?query=workflow%3ACodeQL+branch%3Amaster)
[![Lint](https://github.com/dorssel/dotnet-debounce/actions/workflows/lint.yml/badge.svg?branch=master)](https://github.com/dorssel/dotnet-debounce/actions?query=workflow%3ALint+branch%3Amaster)
[![REUSE status](https://api.reuse.software/badge/github.com/dorssel/dotnet-debounce)](https://api.reuse.software/info/github.com/dorssel/dotnet-debounce)
[![Codecov](https://codecov.io/gh/dorssel/dotnet-debounce/branch/master/graph/badge.svg?token=L0QI0AZRJI)](https://codecov.io/gh/dorssel/dotnet-debounce)
[![NuGet](https://img.shields.io/nuget/v/Dorssel.Utilities.Debounce?logo=nuget)](https://www.nuget.org/packages/Dorssel.Utilities.Debounce)

This library exposes a single object: an event debouncer. It can be used to "filter" or "buffer" multiple incoming events into one.
It is also able to buffer attached event data for later usage at a performance cost.
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
(such as when the user types continuously). Therefore, the debouncer has a configurable maximum
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

### batch processing of data

You listen on a stream that fires a lot of events with data attached. You would like to space out the processing of these events in time,
processing them in batches. By using the generic version of the Debouncer, it is possible to trigger events with data attached.
Retrieve the list of original triggered data by accessing the `TriggerData` property of the `DebouncedEventArgs`.
Multiple data types are supported as long as they have a common base class, such as `EventArgs` for compatibility with .NET events.

## Performance

The library was written with performance in mind. The `Trigger` function (to indicate that a source event has arrived) can easily handle
tens of millions of calls per second, coming in from multiple threads.

Once configured, the debouncer works without object allocation while debouncing.

When triggering the debouncer with data attached, it comes with a significant performance cost because of memory allocations and locking.

## Versatility

The library targets `netstandard2.0` and is compiled for `AnyCPU`.

The public API uses the interface design pattern, which allows 100% code coverage testing with mocking (even for edge cases that will
never occur in real life).

## NuGet package

The released [NuGet package](https://www.nuget.org/packages/Dorssel.Utilities.Debounce)
and the .NET assemblies contained therein have the following properties:

- [Strong Naming](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/strong-naming)
- [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink)
- [IntelliSense](https://learn.microsoft.com/en-us/visualstudio/ide/using-intellisense)
- [Authenticode](https://learn.microsoft.com/en-us/windows/win32/seccrypto/time-stamping-authenticode-signatures#a-brief-introduction-to-authenticode)

## Code Examples

The repository contains two examples:

- A Blazor server-side push example that demonstrates the performance of the library.

- An example that demonstrates how you can get 100% code coverage for classes that use the debouncer.
