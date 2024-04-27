// SPDX-FileCopyrightText: 2024 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT
//
// SPDX-FileContributor: Alain van den Berg

using Dorssel.Utilities.Generic;

namespace Dorssel.Utilities;

/// <summary>
/// This interface specifies the public API for the non-buffering <see cref="Debouncer"/> class.
/// </summary>
public interface IDebouncer : IDebouncer<Void>
{
}
