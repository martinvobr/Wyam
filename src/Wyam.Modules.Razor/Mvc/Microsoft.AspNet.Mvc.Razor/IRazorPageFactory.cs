﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Wyam.Modules.Razor.Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Defines methods that are used for creating <see cref="IRazorPage"/> instances at a given path.
    /// </summary>
    public interface IRazorPageFactory
    {
        /// <summary>
        /// Creates a <see cref="IRazorPage"/> for the specified path.
        /// </summary>
        /// <param name="relativePath">The path to locate the page.</param>
        /// <returns>The IRazorPage instance if it exists, null otherwise.</returns>
        IRazorPage CreateInstance(string relativePath);

        // Creates a IRazorPage for the specified path that uses the specified stream
        IRazorPage CreateInstance(string relativePath, Stream stream);
    }
}
