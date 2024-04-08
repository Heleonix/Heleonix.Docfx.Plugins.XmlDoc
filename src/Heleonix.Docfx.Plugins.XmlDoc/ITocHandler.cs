// <copyright file="ITocHandler.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using System.Collections.Generic;
using global::Docfx.Plugins;

/// <summary>
/// Handles Table of Contents actions.
/// </summary>
public interface ITocHandler
{
    /// <summary>
    /// Builds TOC restructures and adds into the <paramref name="restructions"/>.
    /// </summary>
    /// <param name="model">Content model of a file to handle.</param>
    /// <param name="restructions">Common list of TOC restructures to add handled TOC for <paramref name="model"/>.</param>
    void HandleTocRestructions(FileModel model, IList<TreeItemRestructure> restructions);
}
