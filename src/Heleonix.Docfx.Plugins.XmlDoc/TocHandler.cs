// <copyright file="TocHandler.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using global::Docfx.DataContracts.Common;
using global::Docfx.Plugins;

/// <inheritdoc/>
[Export(nameof(ITocHandler), typeof(ITocHandler))]
public class TocHandler : ITocHandler
{
    /// <inheritdoc/>
    public void HandleTocRestructions(FileModel model, IList<TreeItemRestructure> restructions)
    {
        var content = (IDictionary<string, object>)model.Content;

        var metadata = FileMetadata.From(content);

        if (metadata.Toc.Key == null)
        {
            return;
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

        restructions.Add(new ()
        {
            ActionType = metadata.Toc.Action,
            TypeOfKey = metadata.Toc.Key.StartsWith('~') ? TreeItemKeyType.TopicHref : TreeItemKeyType.TopicUid,
            Key = metadata.Toc.Key,
            RestructuredItems = new List<TreeItem> { treeItem }.ToImmutableList(),
        });
    }
}
