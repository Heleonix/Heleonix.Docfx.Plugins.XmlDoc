// <copyright file="XmlDocProcessor.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using global::Docfx.Common;
using global::Docfx.DataContracts.Common;
using global::Docfx.Plugins;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;

/// <summary>
/// The XmlDocument Processor.
/// </summary>
/// <exclude />
[Export(typeof(IDocumentProcessor))]
public class XmlDocProcessor : IDocumentProcessor
{
    private readonly string[] systemKeys =
    {
        Constants.PropertyName.Conceptual,
        Constants.PropertyName.Type,
        Constants.PropertyName.Source,
        Constants.PropertyName.Path,
        Constants.PropertyName.Documentation,
        Constants.PropertyName.Title,
        "rawTitle",
        "wordCount",
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocProcessor"/> class.
    /// </summary>
    public XmlDocProcessor()
    {
        this.Settings = JsonUtility.Deserialize<Settings>(
            Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".settings.json"));
    }

    /// <summary>
    /// Gets settings loaded from the plugin's .settings.json file.
    /// </summary>
    public Settings Settings { get; private set; }

    /// <summary>
    /// Gets or sets the list of build steps assigned to this processor.
    /// </summary>
    [ImportMany(nameof(XmlDocProcessor))]
    public IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

    /// <summary>
    /// The name of the processor.
    /// </summary>
    public string Name => nameof(XmlDocProcessor);

    /// <summary>
    /// Gets the list of content files recognized for further processing
    /// registered during evaluation of processing priorities.
    /// </summary>
    internal ConcurrentDictionary<string, string> ContentFiles { get; } = new ();

    /// <summary>
    /// Identifies whether the proposed <paramref name="file"/> is one of supported to be processed by this plugin.
    /// </summary>
    /// <param name="file">A file to identify if it can be processed by thi plugin.</param>
    /// <returns>The <see cref="ProcessingPriority.Normal"/> if the <paramref name="file"/> is supported,
    /// otherwise <see cref="ProcessingPriority.NotSupported"/>.</returns>
    public ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type == DocumentType.Article && this.Settings.SupportedFormats.Contains(Path.GetExtension(file.File)))
        {
            this.ContentFiles.TryAdd(file.File, string.Empty);

            return ProcessingPriority.Normal;
        }

        return ProcessingPriority.NotSupported;
    }

    /// <summary>
    /// If <c>hx.xmldoc.store</c> is specified, copies the <paramref name="file"/> into the store.
    /// Creates the FileModel instance for further processing.
    /// </summary>
    /// <param name="file">The content file information to create <see cref="FileModel"/> for.</param>
    /// <param name="metadata">The <c>fileMetadata</c> specified for this <paramref name="file"/> in the <c>docfx.json</c>.</param>
    /// <returns>The FileModel instance for further processing.</returns>
    public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        var finalFileAndType = file;

        var fileMetadata = FileMetadata.From(metadata);

        if (fileMetadata.Store != null)
        {
            // Flat list of files in the store folder. Folder hierarchy is not supported for now.
            finalFileAndType = new FileAndType(
                file.BaseDir,
                Path.Combine(fileMetadata.Store, Path.GetFileName(file.File)),
                file.Type,
                null,
                file.DestinationDir);

            // Copy a content file to the store or update the content of the existing stored file.
            EnvironmentContext.FileAbstractLayer.Copy(file.FullPath, finalFileAndType.FullPath);

            // Remove the information about the original file, because the one from the store wiil be used.
            this.ContentFiles.TryRemove(file.File, out _);

            if (this.ContentFiles.ContainsKey(finalFileAndType.File))
            {
                // Here we have both the original and the stored content files.
                // Previously stored file will be handled with its own FileModel, but with updated content above.
                return null;
            }

            this.ContentFiles.TryAdd(finalFileAndType.File, string.Empty);
        }

        var content = new Dictionary<string, object>
        {
            [Constants.PropertyName.Conceptual] = string.Empty,
            [Constants.PropertyName.Type] = Constants.DocumentType.Conceptual,
            [Constants.PropertyName.Source] = string.Empty,
            [Constants.PropertyName.Path] = finalFileAndType.File,
        };

        foreach (var (key, value) in metadata.OrderBy(item => item.Key))
        {
            content[key] = value;
        }

        content[Constants.PropertyName.SystemKeys] = this.systemKeys;

        var localPathFromRoot = PathUtility.MakeRelativePath(
            EnvironmentContext.BaseDirectory,
            EnvironmentContext.FileAbstractLayer.GetPhysicalPath(finalFileAndType.File));

        return new FileModel(finalFileAndType, content)
        {
            LocalPathFromRoot = localPathFromRoot,
        };
    }

    /// <summary>
    /// Creates a <see cref="SaveResult"/> instance to save the <see cref="FileModel"/> build.
    /// </summary>
    /// <param name="model">The build model with the HTML output to render.</param>
    /// <returns>The saved result with HTML content to render.</returns>
    public SaveResult Save(FileModel model)
    {
        if (!this.ContentFiles.ContainsKey(model.FileAndType.File))
        {
            return null;
        }

        var result = new SaveResult
        {
            DocumentType = Constants.DocumentType.Conceptual,
            FileWithoutExtension = Path.ChangeExtension(model.File, null),
            LinkToFiles = model.LinkToFiles.ToImmutableArray(),
            LinkToUids = model.LinkToUids,
            FileLinkSources = model.FileLinkSources,
            UidLinkSources = model.UidLinkSources,
        };

        if (model.Properties.XrefSpec != null)
        {
            result.XRefSpecs =
                ImmutableArray.Create((model.Properties as IDictionary<string, object>)["XrefSpec"] as XRefSpec);
        }

        return result;
    }

    /// <summary>
    /// Updates references in the <paramref name="model"/> file model.
    /// </summary>
    /// <param name="model">The model to update references for.</param>
    /// <param name="context">The context with common functionality to update references for <paramref name="model"/>.</param>
    public void UpdateHref(FileModel model, IDocumentBuildContext context)
    {
        // Not used.
    }
}