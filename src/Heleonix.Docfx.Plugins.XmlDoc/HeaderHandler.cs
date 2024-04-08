// <copyright file="HeaderHandler.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using global::Docfx.Common;
using global::Docfx.DataContracts.Common;
using global::Docfx.Plugins;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Net;

/// <inheritdoc/>>
[Export(nameof(IHeaderHandler), typeof(IHeaderHandler))]
public class HeaderHandler : IHeaderHandler
{
    /// <inheritdoc/>
    public (string h1, string h1Raw, string body) ExtractH1(string html)
    {
        var document = new HtmlDocument();

        document.LoadHtml(html);

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

    /// <inheritdoc/>
    public void HandleYamlHeader(ImmutableDictionary<string, object> yamlHeader, FileModel model)
    {
        if (yamlHeader == null)
        {
            return;
        }

        foreach (var item in yamlHeader.OrderBy(i => i.Key, StringComparer.Ordinal))
        {
            var content = (IDictionary<string, object>)model.Content;

            switch (item.Key)
            {
                case Constants.PropertyName.Uid:
                    var uid = item.Value as string;

                    if (!string.IsNullOrWhiteSpace(uid))
                    {
                        content[item.Key] = item.Value;
                        model.Uids = new[] { new UidDefinition(uid, model.LocalPathFromRoot) }.ToImmutableArray();
                    }

                    break;
                case Constants.PropertyName.DocumentType:
                    content[item.Key] = item.Value;
                    model.DocumentType = item.Value as string;

                    break;
                case Constants.PropertyName.OutputFileName:
                    content[item.Key] = item.Value;

                    var outputFileName = item.Value as string;

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
                    content[item.Key] = item.Value;

                    break;
            }
        }
    }

    /// <inheritdoc/>
    public string GetTitle(FileModel model, ImmutableDictionary<string, object> yamlHeader, string h1)
    {
        // title from YAML header
        if (yamlHeader != null && TryGetStringValue(yamlHeader, Constants.PropertyName.Title, out var yamlHeaderTitle))
        {
            return yamlHeaderTitle;
        }

        var content = (IDictionary<string, object>)model.Content;

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

        return Path.GetFileNameWithoutExtension(model.File);
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
