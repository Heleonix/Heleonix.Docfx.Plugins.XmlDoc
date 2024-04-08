// <copyright file="TocHandlerTests.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc.Tests;

using global::Docfx.DataContracts.Common;
using Moq;

/// <summary>
/// Tests the <see cref="TocHandler"/>.
/// </summary>
[ComponentTest(Type = typeof(TocHandler))]
public class TocHandlerTests
{
    /// <summary>
    /// Tests the <see cref="TocHandler.HandleTocRestructions"/>.
    /// </summary>
    [MemberTest(Name = nameof(TocHandler.HandleTocRestructions))]
    public void HandleTocRestructions()
    {
        var tocHandler = new TocHandler();
        FileModel model = null;
        IList<TreeItemRestructure> restructions = null;

        When("the method is called", () =>
        {
            Arrange(() =>
            {
                restructions = new List<TreeItemRestructure>();
            });

            Act(() =>
            {
                tocHandler.HandleTocRestructions(model, restructions);
            });

            And("metadata TOC key is null", () =>
            {
                Arrange(() =>
                {
                    model = new FileModel(
                        new FileAndType("X:/BaseDir", "file.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                            {
                                FileMetadata.TocKey,
                                new Dictionary<string, object>
                                {
                                    { "action", "AppendChild" },
                                    { "key", null },
                                }
                            },
                        });
                });

                Should("do nothing", () =>
                {
                    Assert.That(restructions, Is.Empty);
                });
            });

            And("metadata TOC Key is Href", () =>
            {
                Arrange(() =>
                {
                    model = new FileModel(
                        new FileAndType("X:/BaseDir", "file.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                                { Constants.PropertyName.Title, "Title" },
                                { "_appName", "App name" },
                                { "_appTitle", "App title" },
                                { "_enableSearch", "Enable search" },
                                {
                                    FileMetadata.TocKey,
                                    new Dictionary<string, object>
                                    {
                                        { "action", "AppendChild" },
                                        { "key", "~/some/path" },
                                    }
                                },
                        });
                });

                Should("generate one TOC restructure with the specified Href key", () =>
                {
                    var treeItem = restructions.Single().RestructuredItems.Single();

                    Assert.That(treeItem.Metadata[Constants.PropertyName.Name], Is.EqualTo("Title"));
                    Assert.That(treeItem.Metadata[Constants.PropertyName.Href], Is.EqualTo("~/file.xsd"));
                    Assert.That(treeItem.Metadata[Constants.PropertyName.TopicHref], Is.EqualTo("~/file.xsd"));
                    Assert.That(restructions.Single().Key, Is.EqualTo("~/some/path"));
                    Assert.That(restructions.Single().TypeOfKey, Is.EqualTo(TreeItemKeyType.TopicHref));
                    Assert.That(restructions.Single().ActionType, Is.EqualTo(TreeItemActionType.AppendChild));
                });
            });

            And("metadata TOC Key is Uid", () =>
            {
                Arrange(() =>
                {
                    model = new FileModel(
                        new FileAndType("X:/BaseDir", "file.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                                { Constants.PropertyName.Title, "Title" },
                                { "_appName", "App name" },
                                { "_appTitle", "App title" },
                                { "_enableSearch", "Enable search" },
                                {
                                    FileMetadata.TocKey,
                                    new Dictionary<string, object>
                                    {
                                        { "action", "AppendChild" },
                                        { "key", "some.key" },
                                    }
                                },
                        });
                });

                Should("generate one TOC restructure with the specified Uid key", () =>
                {
                    var treeItem = restructions.Single().RestructuredItems.Single();

                    Assert.That(treeItem.Metadata[Constants.PropertyName.Name], Is.EqualTo("Title"));
                    Assert.That(treeItem.Metadata[Constants.PropertyName.Href], Is.EqualTo("~/file.xsd"));
                    Assert.That(treeItem.Metadata[Constants.PropertyName.TopicHref], Is.EqualTo("~/file.xsd"));
                    Assert.That(restructions.Single().Key, Is.EqualTo("some.key"));
                    Assert.That(restructions.Single().TypeOfKey, Is.EqualTo(TreeItemKeyType.TopicUid));
                    Assert.That(restructions.Single().ActionType, Is.EqualTo(TreeItemActionType.AppendChild));
                });
            });
        });
    }
}
