// <copyright file="IHeaderHandler.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using System.Collections.Immutable;
using global::Docfx.Plugins;

/// <summary>
/// Handles headers and titles of the generated html result content.
/// </summary>
public interface IHeaderHandler
{
    /// <summary>
    /// Extracts the 'h1' header from the html result content and returns both.
    /// </summary>
    /// <param name="html">The html result content to extract 'h1' header from.</param>
    /// <returns>Extracted 'h1' header and content.</returns>
    (string h1, string h1Raw, string body) ExtractH1(string html);

    /// <summary>
    /// Handles the yaml header of the markdown to apply defined values in the yaml header.
    /// </summary>
    /// <param name="yamlHeader">The yaml header fo the html result contents of the
    /// <paramref name="model"/> to handle.</param>
    /// <param name="model">The model of the markdown content to handle the yaml header for.</param>
    void HandleYamlHeader(ImmutableDictionary<string, object> yamlHeader, FileModel model);

    /// <summary>
    /// Gets a title from the passed sources.
    /// </summary>
    /// <param name="model">A model to try to get a title from.</param>
    /// <param name="yamlHeader">A Yaml Header of the markdown file to get a title from.</param>
    /// <param name="h1">A 'h1' header to return as a title.</param>
    /// <returns>A title from the provided sources.</returns>
    string GetTitle(FileModel model, ImmutableDictionary<string, object> yamlHeader, string h1);
}
