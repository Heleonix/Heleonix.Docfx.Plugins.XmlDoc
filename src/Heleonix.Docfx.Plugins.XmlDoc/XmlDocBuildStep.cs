// <copyright file="XmlDocBuildStep.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using global::Docfx.Common;
using global::Docfx.DataContracts.Common;
using global::Docfx.Plugins;

/// <summary>
/// The build step to generate html documentation from the xml-based content files.
/// </summary>
/// <exclude />
[Export(nameof(XmlDocProcessor), typeof(IDocumentBuildStep))]
public class XmlDocBuildStep : IDocumentBuildStep
{
    /// <summary>
    /// Gets or sets the transformer to use to transform xml-based contents into markdown.
    /// </summary>
    [Import(nameof(ITransformer))]
    public ITransformer Transformer { get; set; }

    /// <summary>
    /// Gets or sets the header handler to handle headers and titles of the generated html result content.
    /// </summary>
    [Import(nameof(IHeaderHandler))]
    public IHeaderHandler HeaderHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler of Table of Contents actions specified for xml-based files.
    /// </summary>
    [Import(nameof(ITocHandler))]
    public ITocHandler TocHandler { get; set; }

    /// <summary>
    /// Gets the order of the build step to be executed with.
    /// </summary>
    public int BuildOrder => 0;

    /// <summary>
    /// Gets the name of the build step.
    /// </summary>
    public string Name => nameof(XmlDocBuildStep);

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <param name="model">The model to be built.</param>
    /// <param name="host">The host to be used for common tasks.</param>
    public void Build(FileModel model, IHostService host)
    {
        // Since html output of the content files is needed in the PreBuild stage to create toc restructures
        // with the extracted 'h1' header, this method is not used.
        // All operations are implemented in the PreBuild method instead.
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <param name="models">The models of content files to process after build.</param>
    /// <param name="host">The host to be used for common tasks.</param>
    public void Postbuild(ImmutableList<FileModel> models, IHostService host)
    {
        // No actions needed at the post-build stage.
    }

    /// <summary>
    /// Builds the xml-based contents via transformations into Markdown.
    /// </summary>
    /// <param name="models">The models to transform from XML into HTML output.</param>
    /// <param name="host">The host to be used for common tasks.</param>
    /// <returns>The processed models.</returns>
    public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
    {
        var processor = (XmlDocProcessor)host.Processor;

        var tocRestructions = new List<TreeItemRestructure>();

        foreach (var model in models)
        {
            if (!processor.ContentFiles.ContainsKey(model.FileAndType.File))
            {
                continue;
            }

            var content = (Dictionary<string, object>)model.Content;

            var markdown = this.Transformer.Transform(model, host);

            var result = host.Markup(markdown, model.FileAndType, false);

            var (h1, h1Raw, conceptual) = this.HeaderHandler.ExtractH1(result.Html);

            content["rawTitle"] = h1Raw;

            if (!string.IsNullOrEmpty(h1Raw))
            {
                (model.ManifestProperties as IDictionary<string, object>)["rawTitle"] = h1Raw;
            }

            content[Constants.PropertyName.Conceptual] = conceptual;

            this.HeaderHandler.HandleYamlHeader(result.YamlHeader, model);

            content[Constants.PropertyName.Title] = this.HeaderHandler.GetTitle(model, result.YamlHeader, h1);

            model.LinkToFiles = result.LinkToFiles.ToImmutableHashSet();
            model.LinkToUids = result.LinkToUids;
            model.FileLinkSources = result.FileLinkSources;
            model.UidLinkSources = result.UidLinkSources;
            (model.Properties as IDictionary<string, object>)["XrefSpec"] = null;

            if (model.Uids.Length > 0)
            {
                var xrefSpec = new XRefSpec
                {
                    Uid = model.Uids[0].Name,
                    Name = content[Constants.PropertyName.Title] as string,
                    Href = ((RelativePath)model.File).GetPathFromWorkingFolder(),
                };

                (model.Properties as IDictionary<string, object>)["XrefSpec"] = xrefSpec;
            }

            this.TocHandler.HandleTocRestructions(model, tocRestructions);
        }

        host.TableOfContentRestructions = tocRestructions.ToImmutableList();

        return models;
    }
}
