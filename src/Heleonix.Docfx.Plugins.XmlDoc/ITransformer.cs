// <copyright file="ITransformer.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using global::Docfx.Plugins;

/// <summary>
/// Declares functionality to transform xml-based files into markdown using different templates.
/// </summary>
public interface ITransformer
{
    /// <summary>
    /// Transforms the specified model with the template specified in metadata.
    /// </summary>
    /// <param name="model">The xml model to transform.</param>
    /// <param name="host">A host service from Docfx.</param>
    /// <returns>The string containing the transformed contents.</returns>
    string Transform(FileModel model, IHostService host);
}