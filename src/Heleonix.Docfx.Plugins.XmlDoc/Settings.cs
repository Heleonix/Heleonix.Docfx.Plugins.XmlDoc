// <copyright file="Settings.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

/// <summary>
/// Represents the settings of this plugin, specified in the <c>Heleonix.Docfx.Plugins.XmlDoc.settings.json</c>.
/// </summary>
public class Settings
{
    /// <summary>
    /// Represents the list of file extensions to be recognized and processed by this plugin.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///   "SupportedFormats": [ ".xml", ".xsd", ".yourformat" ]
    /// }
    /// </code>
    /// </example>
    public string[] SupportedFormats { get; set; }
}
