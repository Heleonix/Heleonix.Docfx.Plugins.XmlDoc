// <copyright file="FileMetadataTests.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc.Tests
{
    using global::Docfx.Plugins;

    /// <summary>
    /// Tests the <see cref="FileMetadata"/>.
    /// </summary>
    [ComponentTest(Type = typeof(FileMetadata))]
    public static class FileMetadataTests
    {
        /// <summary>
        /// Tests the <see cref="FileMetadata.From(IDictionary{string, object})"/>.
        /// </summary>
        [MemberTest(Name = nameof(FileMetadata.From))]
        public static void From()
        {
            IDictionary<string, object> dictionary = null;
            FileMetadata metadata = null;

            When("the method is called", () =>
            {
                Act(() =>
                {
                    metadata = FileMetadata.From(dictionary);
                });

                And("the untyped contents is null", () =>
                {
                    dictionary = null;

                    Should("return the empty FileMetadata instance", () =>
                    {
                        Assert.That(metadata.Store, Is.Null);
                        Assert.That(metadata.Template, Is.Null);
                        Assert.That(metadata.Toc, Is.Null);
                    });
                });

                And("the untyped contents is provided", () =>
                {
                    dictionary = new Dictionary<string, object>
                    {
                        { FileMetadata.StoreKey, "./store" },
                        { FileMetadata.TemplateKey, "./transform.xslt" },
                        {
                            FileMetadata.TocKey, new Dictionary<string, object>
                            {
                                { "key", "SomeToc" },
                                { "action", "InsertAfter" },
                            }
                        },
                    };

                    Should("return the filled in FileMetadata instance", () =>
                    {
                        Assert.That(metadata.Store, Is.EqualTo("./store"));
                        Assert.That(metadata.Template, Is.EqualTo("./transform.xslt"));
                        Assert.That(metadata.Toc.Key, Is.EqualTo("SomeToc"));
                        Assert.That(metadata.Toc.Action, Is.EqualTo(TreeItemActionType.InsertAfter));
                    });
                });

                And("the TOC is not provided", () =>
                {
                    dictionary = new Dictionary<string, object>
                    {
                        { FileMetadata.StoreKey, "./store" },
                        { FileMetadata.TemplateKey, "./transform.xslt" },
                    };

                    Should("return the filled in FileMetadata instance without TOC", () =>
                    {
                        Assert.That(metadata.Store, Is.EqualTo("./store"));
                        Assert.That(metadata.Template, Is.EqualTo("./transform.xslt"));
                        Assert.That(metadata.Toc.Key, Is.Null);
                        Assert.That(metadata.Toc.Action, Is.EqualTo(TreeItemActionType.ReplaceSelf));
                    });
                });
            });
        }
    }
}
