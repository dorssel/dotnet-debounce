// SPDX-FileCopyrightText: 2022 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

[assembly: CLSCompliant(false)]
[assembly: ExcludeFromCodeCoverage]
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
