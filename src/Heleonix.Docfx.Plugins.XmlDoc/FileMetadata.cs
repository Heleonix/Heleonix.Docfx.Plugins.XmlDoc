// <copyright file="FileMetadata.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using global::Docfx.Plugins;

/// <summary>
/// Represents the model of the supported <c>fileMetadata</c> specified for content files to be processed by this plugin.
/// </summary>
/// <example>
/// An example of the docfx.json:
/// <code>
/// {
///   "build": {
///     "content": [
///       {
///         "files": [ "**/*.{md,yml}" ],
///         "exclude": [ "_site/**" ]
///       },
///       {
///         "files": [ "*.xsd" ],
///         "src": "../../some-external-location"
///       },
///       {
///         "files": [ "internal-store-folder/*.xsd" ]
///       }
///     ],
///     "resource": [
///       {
///         "files": [ "images/**" ]
///       }
///     ],
///     "output": "_site",
///     "template": [
///       "default",
///       "templates/your-template-with"
///     ],
///     "fileMetadata": {
///       "hx.xmldoc.xslt": { "**.xsd": "./xml-to-md.xslt" },
///       "hx.xmldoc.store": { "../../some-external-location/*.xsd": "internal-store-folder" }
///       "hx.xmldoc.toc": {
///         "**/*-some.xsd": { "action": "InsertAfter", "key": "~/articles/introduction.md" },
///         "**/*-other.xsd": { "action": "AppendChild", "key": "Namespace.Class.whatever.uid" }
///       }
///     }
///   }
/// }
/// </code>
/// </example>
public class FileMetadata
{
    /// <summary>
    /// The name of the store key in the <c>fileMetadata</c>  of content files.
    /// </summary>
    public const string StoreKey = "hx.xmldoc.store";

    /// <summary>
    /// The name of the xslt key in the <c>fileMetadata</c> of content files.
    /// </summary>
    public const string XsltKey = "hx.xmldoc.xslt";

    /// <summary>
    /// The name of the Table Of Contents key in the <c>fileMetadata</c>  of content files.
    /// </summary>
    public const string TocKey = "hx.xmldoc.toc";

    /// <summary>
    /// The <c>hx.xmldoc.store</c> relative path to the store to copy files before processing,
    /// so that they are available if the original files are absent,
    /// i.e. if documentation is rebuild in another location.
    /// This can be considered similarly to the metadata yml files generated from external *.csproj files
    /// and then are stored in the documentation folder.
    /// </summary>
    public string Store { get; set; }

    /// <summary>
    /// The <c>hx.xmldoc.xslt</c> file metadata to specify a path to an XSLT file to transform XML-based content files
    /// into Markdown for further generation of HTML output files styled with the documentation templates.
    /// </summary>
    public string Xslt { get; set; }

    /// <summary>
    /// The <c>hx.xmldoc.toc</c> file metadata to specify a configuration for
    /// Table Of Contents in the <see cref="Toc"/> format.
    /// </summary>
    public TocMetadata Toc { get; set; }

    /// <summary>
    /// Creates instance of the <see cref="TocMetadata"/> crom the untyped contents.
    /// </summary>
    /// <param name="dictionary">The untyped contents.</param>
    /// <returns>The instance of the <see cref="TocMetadata"/> parsed from the <paramref name="dictionary"/>.</returns>
    /// <exclude/>
    public static FileMetadata From(IDictionary<string, object> dictionary)
    {
        var metadata = new FileMetadata();

        if (dictionary == null)
        {
            return metadata;
        }

        dictionary.TryGetValue(FileMetadata.StoreKey, out var obj);
        metadata.Store = obj as string;

        dictionary.TryGetValue(FileMetadata.XsltKey, out obj);
        metadata.Xslt = obj as string;

        dictionary.TryGetValue(FileMetadata.TocKey, out var tocObj);
        metadata.Toc = TocMetadata.From(tocObj as IDictionary<string, object>);

        return metadata;
    }

    /// <summary>
    /// This metadata represents configuration to add the content file into a Table OF Contents.
    /// </summary>
    public class TocMetadata
    {
        /// <summary>
        /// Specifies <c>action</c> in file metadata to add the content file.
        /// See <see cref="TreeItemActionType"/> for possible actions.
        /// The <see cref="TreeItemActionType.DeleteSelf"/>, however, does not make much sense.
        /// </summary>
        public TreeItemActionType Action { get; set; }

        /// <summary>
        /// The <c>key</c> of a Table OF Contents to add the content file with the specified <see cref="Action"/>.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Creates instance of the <see cref="TocMetadata"/> crom the untyped contents.
        /// </summary>
        /// <param name="dictionary">The untyped contents.</param>
        /// <returns>The instance of the <see cref="TocMetadata"/> parsed from the <paramref name="dictionary"/>.</returns>
        /// <exclude/>
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
        public static TocMetadata From(IDictionary<string, object> dictionary)
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
        {
            var metadata = new TocMetadata();

            if (dictionary == null)
            {
                return metadata;
            }

            dictionary.TryGetValue("action", out var obj);
            metadata.Action = (TreeItemActionType)Enum.Parse(typeof(TreeItemActionType), obj as string);

            dictionary.TryGetValue("key", out obj);
            metadata.Key = obj as string;

            return metadata;
        }
    }
}
