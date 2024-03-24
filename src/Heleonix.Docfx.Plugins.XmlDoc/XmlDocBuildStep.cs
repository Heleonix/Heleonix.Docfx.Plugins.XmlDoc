// <copyright file="XmlDocBuildStep.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Xsl;
using global::Docfx.Common;
using global::Docfx.DataContracts.Common;
using global::Docfx.Plugins;
using HtmlAgilityPack;

/// <summary>
/// The build step to generate html documentation from the xml-based content files.
/// </summary>
/// <exclude />
[Export(nameof(XmlDocProcessor), typeof(IDocumentBuildStep))]
public class XmlDocBuildStep : IDocumentBuildStep
{
    private readonly ConcurrentDictionary<string, XslCompiledTransform> transforms = new ();

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocBuildStep"/> class.
    /// </summary>
    public XmlDocBuildStep()
    {
        AssemblyLoadContext.Default.Resolving += XmlDocBuildStep.Default_Resolving;
    }

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
    /// Builds the xml-based contents via Markdown XSLT transformation into HTML output.
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

            var metadata = FileMetadata.From(content);

            var transform = this.transforms.GetOrAdd(
                metadata.Xslt,
                (k, arg) =>
                {
                    var t = new XslCompiledTransform();
                    t.Load(arg);
                    return t;
                }, metadata.Xslt);

            using (var stringWriter = new StringWriter())
            {
                var args = new XsltArgumentList();

                args.AddParam("filename", string.Empty, Path.GetFileNameWithoutExtension(model.File));

                transform.Transform(model.FileAndType.FullPath, args, stringWriter);

                content[Constants.PropertyName.Conceptual] = stringWriter.ToString();
            }

            var markdown = (string)content[Constants.PropertyName.Conceptual];

            var result = host.Markup(markdown, model.FileAndType, false);

            var (h1, h1Raw, conceptual) = XmlDocBuildStep.ExtractH1(result.Html);

            content["rawTitle"] = h1Raw;

            if (!string.IsNullOrEmpty(h1Raw))
            {
                model.ManifestProperties.rawTitle = h1Raw;
            }

            content[Constants.PropertyName.Conceptual] = conceptual;

            if (result.YamlHeader != null)
            {
                foreach (var item in result.YamlHeader.OrderBy(i => i.Key, StringComparer.Ordinal))
                {
                    XmlDocBuildStep.HandleYamlHeaderPair(model, item.Key, item.Value);
                }
            }

            content[Constants.PropertyName.Title] =
                XmlDocBuildStep.GetTitle(content, result.YamlHeader, h1)
                ?? Path.GetFileNameWithoutExtension(model.File);

            model.LinkToFiles = result.LinkToFiles.ToImmutableHashSet();
            model.LinkToUids = result.LinkToUids;
            model.FileLinkSources = result.FileLinkSources;
            model.UidLinkSources = result.UidLinkSources;
            model.Properties.XrefSpec = null;

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

            if (metadata.Toc.Key == null)
            {
                continue;
            }

            var treeItem = new TreeItem();

            treeItem.Metadata[Constants.PropertyName.Name] = content[Constants.PropertyName.Title];
            treeItem.Metadata[Constants.PropertyName.Href] = model.Key;
            treeItem.Metadata[Constants.PropertyName.TopicHref] = model.Key;

            if (content.ContainsKey("_appName"))
            {
                treeItem.Metadata["_appName"] = content["_appName"];
            }

            if (content.ContainsKey("_appTitle"))
            {
                treeItem.Metadata["_appTitle"] = content["_appTitle"];
            }

            if (content.ContainsKey("_enableSearch"))
            {
                treeItem.Metadata["_enableSearch"] = content["_enableSearch"];
            }

            tocRestructions.Add(new ()
            {
                ActionType = metadata.Toc.Action,
                TypeOfKey = metadata.Toc.Key.StartsWith("~") ? TreeItemKeyType.TopicHref : TreeItemKeyType.TopicUid,
                Key = metadata.Toc.Key,
                RestructuredItems = new List<TreeItem> { treeItem }.ToImmutableList(),
            });
        }

        host.TableOfContentRestructions = tocRestructions.ToImmutableList();

        return models;
    }

    [ExcludeFromCodeCoverage]
    private static Assembly Default_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        if (assemblyName.Name.Equals("HtmlAgilityPack", StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "HtmlAgilityPack.dll");

#pragma warning disable S3885 // "Assembly.Load" should be used
            return Assembly.LoadFile(path);
#pragma warning restore S3885 // "Assembly.Load" should be used
        }

        return null;
    }

    private static (string h1, string h1Raw, string body) ExtractH1(string contentHtml)
    {
        var document = new HtmlDocument();

        document.LoadHtml(contentHtml);

        // InnerText in HtmlAgilityPack is not decoded, should be a bug
        var h1Node = document.DocumentNode.SelectSingleNode("//h1");
        var h1 = WebUtility.HtmlDecode(h1Node?.InnerText);
        var h1Raw = string.Empty;

        // If the html content is a fragment, which starts with 'h1' heading, like: <h1>Heading</h1><p>Content</p>
        if (h1Node != null && GetFirstNoneCommentChild(document.DocumentNode) == h1Node)
        {
            h1Raw = h1Node.OuterHtml;
            h1Node.Remove();
        }

        return (h1, h1Raw, document.DocumentNode.OuterHtml);

        static HtmlNode GetFirstNoneCommentChild(HtmlNode node)
        {
            var result = node.FirstChild;

            while (result != null)
            {
                if (result.NodeType == HtmlNodeType.Comment || string.IsNullOrWhiteSpace(result.OuterHtml))
                {
                    result = result.NextSibling;
                }
                else
                {
                    break;
                }
            }

            return result;
        }
    }

    private static void HandleYamlHeaderPair(FileModel model, string key, object value)
    {
        var content = (IDictionary<string, object>)model.Content;

        switch (key)
        {
            case Constants.PropertyName.Uid:
                var uid = value as string;

                if (!string.IsNullOrWhiteSpace(uid))
                {
                    content[key] = value;
                    model.Uids = new[] { new UidDefinition(uid, model.LocalPathFromRoot) }.ToImmutableArray();
                }

                break;
            case Constants.PropertyName.DocumentType:
                content[key] = value;
                model.DocumentType = value as string;

                break;
            case Constants.PropertyName.OutputFileName:
                content[key] = value;

                var outputFileName = value as string;

                if (!string.IsNullOrWhiteSpace(outputFileName))
                {
                    if (Path.GetFileName(outputFileName) == outputFileName)
                    {
                        model.File = (RelativePath)model.File + (RelativePath)outputFileName;
                    }
                    else
                    {
                        Logger.LogWarning($"Invalid output file name in yaml header: {outputFileName}, skip rename output file.");
                    }
                }

                break;
            default:
                content[key] = value;

                break;
        }
    }

    private static string GetTitle(IDictionary<string, object> content, ImmutableDictionary<string, object> yamlHeader, string h1)
    {
        // title from YAML header
        if (yamlHeader != null
            && TryGetStringValue(yamlHeader, Constants.PropertyName.Title, out var yamlHeaderTitle))
        {
            return yamlHeaderTitle;
        }

        // title from metadata/titleOverwriteH1
        if (TryGetStringValue(content, Constants.PropertyName.TitleOverwriteH1, out var titleOverwriteH1))
        {
            return titleOverwriteH1;
        }

        // title from H1
        if (!string.IsNullOrEmpty(h1))
        {
            return h1;
        }

        // title from globalMetadata or fileMetadata
        if (TryGetStringValue(content, Constants.PropertyName.Title, out var title))
        {
            return title;
        }

        return null;
    }

    private static bool TryGetStringValue(IDictionary<string, object> dictionary, string key, out string strValue)
    {
        if (dictionary.TryGetValue(key, out var value) && value is string str && !string.IsNullOrEmpty(str))
        {
            strValue = str;

            return true;
        }
        else
        {
            strValue = null;

            return false;
        }
    }
}
