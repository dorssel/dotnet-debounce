<!--
SPDX-FileCopyrightText: 2023 Frans van Dorsselaer

SPDX-License-Identifier: MIT
-->

# dotnet-aes-extra

There are two builds of the library included in th package, one for .NET Standard 2.0 and one for .NET 8 (or higher).
The public @"Dorssel.Utilities.Debouncer?text=API" is the same for both, but internally the builds slightly differ:

- The .NET Standard build depends on `Microsoft.Bcl.TimeProvider` for unit testing.

- The .NET 8 build supports trimming.
