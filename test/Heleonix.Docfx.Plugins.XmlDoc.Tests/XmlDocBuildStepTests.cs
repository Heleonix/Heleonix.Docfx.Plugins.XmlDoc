// <copyright file="XmlDocBuildStepTests.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc.Tests;

using global::Docfx.DataContracts.Common;
using global::Docfx.Plugins;
using Heleonix.Docfx.Plugins.XmlDoc;
using Moq;
using System.Collections.Immutable;

/// <summary>
/// Tests the <see cref="XmlDocBuildStep"/>.
/// </summary>
[ComponentTest(Type = typeof(XmlDocBuildStep))]
public static class XmlDocBuildStepTests
{
    /// <summary>
    /// Tests the <see cref="XmlDocBuildStep.Prebuild(ImmutableList{FileModel}, IHostService)"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocBuildStep.Prebuild))]
    public static void Prebuild()
    {
        var xmlDocBuildStep = new XmlDocBuildStep();
        var hostMock = new Mock<IHostService>();
        var ft = new FileAndType(Environment.CurrentDirectory, "Input.xsd", DocumentType.Article);
        ImmutableList<FileModel> models = null;
        string contentHtml = null;
        ImmutableDictionary<string, object> yamlHeader = null;
        XmlDocProcessor processor = null;
        FileModel result = null;
        Dictionary<string, object> metadata = null;

        metadata = new Dictionary<string, object>
        {
            { FileMetadata.XsltKey, Path.Combine(Environment.CurrentDirectory, "Template.xslt") },
        };

        Arrange(() =>
        {
            processor = new XmlDocProcessor();

            processor.GetProcessingPriority(ft);

            var model = processor.Load(ft, metadata.ToImmutableDictionary());

            var markupResult = new MarkupResult
            {
                Html = contentHtml,
                YamlHeader = yamlHeader,
                LinkToFiles = new string[] { "link1" }.ToImmutableArray(),
                FileLinkSources = new Dictionary<string, ImmutableList<LinkSourceInfo>>
                {
                    { "key1", new List<LinkSourceInfo> { new LinkSourceInfo { SourceFile = "src1" } }.ToImmutableList() },
                }.ToImmutableDictionary(),
                LinkToUids = new HashSet<string> { "uid1" }.ToImmutableHashSet(),
                UidLinkSources = new Dictionary<string, ImmutableList<LinkSourceInfo>>
                {
                    { "key2", new List<LinkSourceInfo> { new LinkSourceInfo { SourceFile = "src2" } }.ToImmutableList() },
                }.ToImmutableDictionary(),
            };

            models = new List<FileModel> { model }.ToImmutableList();

            hostMock.SetupGet((IHostService hostService) => hostService.Processor).Returns(processor);
            hostMock.Setup((IHostService hostService) => hostService.Markup(
                It.Is<string>(c => c.Contains("### Properties")), ft, false))
                .Returns(markupResult);
        });

        When("the method is called", () =>
        {
            Act(() =>
            {
                result = xmlDocBuildStep.Prebuild(models, hostMock.Object).Single();
            });

            And("there is a full html content", () =>
            {
                contentHtml = "<!DOCTYPE html><html><head></head><body><h1>Heading</h1><p>Content</p></body></html>";

                Should("generate html content", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Conceptual], Contains.Substring("Content"));
                    Assert.That(content[Constants.PropertyName.Title], Contains.Substring("Heading"));
                    Assert.That(result.ManifestProperties.rawTitle, Is.Null);
                    Assert.That(result.LinkToFiles, Contains.Item("link1"));
                    Assert.That(result.LinkToUids, Contains.Item("uid1"));
                    Assert.That(result.FileLinkSources["key1"][0], Is.EqualTo("src1"));
                    Assert.That(result.FileLinkSources["key2"][0], Is.EqualTo("src2"));
                    Assert.That(result.Properties.XrefSpec, Is.Null);
                });
            });

            And("there is an unrecognized content file", () =>
            {
                Arrange(() =>
                {
                    processor.ContentFiles.Remove(ft.File, out _);
                });

                Should("skip the unrecognized file", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Conceptual], Is.Empty);
                });
            });

            And("Table Of Contents is specified in metadata to be added after Href item", () =>
            {
                metadata = new Dictionary<string, object>
                {
                    { FileMetadata.XsltKey, Path.Combine(Environment.CurrentDirectory, "Template.xslt") },
                    {
                        FileMetadata.TocKey,
                        new Dictionary<string, object>
                        {
                            { "action", "InsertAfter" },
                            { "key", "~/articles/introduction.md" },
                        }
                    },
                    { "_appName", "App Name" },
                    { "_appTitle", "App Title" },
                    { "_enableSearch", true },
                };

                Should("generate html content with TOC restructions", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Conceptual], Contains.Substring("Content"));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].ActionType,
                        Is.EqualTo(TreeItemActionType.InsertAfter));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].Key,
                        Is.EqualTo("~/articles/introduction.md"));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].TypeOfKey,
                        Is.EqualTo(TreeItemKeyType.TopicHref));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].RestructuredItems[0].Metadata["name"],
                        Is.EqualTo("Heading"));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].RestructuredItems[0].Metadata["_appName"],
                        Is.EqualTo("App Name"));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].RestructuredItems[0].Metadata["_appTitle"],
                        Is.EqualTo("App Title"));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].RestructuredItems[0].Metadata["_enableSearch"],
                        Is.True);
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].RestructuredItems[0].Metadata[Constants.PropertyName.Href],
                        Is.EqualTo("~/articles/introduction.md"));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].RestructuredItems[0].Metadata[Constants.PropertyName.TopicHref],
                        Is.EqualTo("~/articles/introduction.md"));
                });
            });

            And("Table Of Contents is specified in metadata to be added after Uid item", () =>
            {
                metadata = new Dictionary<string, object>
                {
                    { FileMetadata.XsltKey, Path.Combine(Environment.CurrentDirectory, "Template.xslt") },
                    {
                        FileMetadata.TocKey,
                        new Dictionary<string, object>
                        {
                            { "action", "InsertAfter" },
                            { "key", "introduction" },
                        }
                    },
                    { "_appName", "App Name" },
                    { "_appTitle", "App Title" },
                    { "_enableSearch", true },
                };

                Should("generate html content with TOC restructions", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Conceptual], Contains.Substring("Content"));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].ActionType,
                        Is.EqualTo(TreeItemActionType.InsertAfter));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].Key,
                        Is.EqualTo("introduction"));
                    Assert.That(
                        hostMock.Object.TableOfContentRestructions[0].TypeOfKey,
                        Is.EqualTo(TreeItemKeyType.TopicUid));
                });
            });

            And("the loaded html content is a fragment of html document", () =>
            {
                contentHtml = "     <h1>Heading</h1>     <p>Text</p>";

                Should("generate html content", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Conceptual], Contains.Substring("Text"));
                    Assert.That(content[Constants.PropertyName.Title], Contains.Substring("Heading"));
                });

                And("the html content fragment has a comment", () =>
                {
                    contentHtml = "<!--some comment--><h1>Heading</h1><p>Text</p>";

                    Should("generate html content", () =>
                    {
                        var content = result.Content as IDictionary<string, object>;

                        Assert.That(content[Constants.PropertyName.Conceptual], Contains.Substring("Text"));
                        Assert.That(content[Constants.PropertyName.Title], Contains.Substring("Heading"));
                    });
                });
            });

            And("there is no 'h1' html tag", () =>
            {
                contentHtml = "<p>Text</p>";

                Should("generate html content with the file name as a title", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Conceptual], Contains.Substring("Text"));
                    Assert.That(content[Constants.PropertyName.Title], Contains.Substring("Input"));
                });
            });

            And("the 'h1' title is overwritten in metadata", () =>
            {
                metadata = new Dictionary<string, object>
                {
                    { FileMetadata.XsltKey, Path.Combine(Environment.CurrentDirectory, "Template.xslt") },
                    { Constants.PropertyName.TitleOverwriteH1, "Header Overwrite" },
                };

                Should("generate html content with overridden title", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Title], Is.EqualTo("Header Overwrite"));
                });
            });

            And("there the 'h1' title is overwritten in global metadata", () =>
            {
                metadata = new Dictionary<string, object>
                {
                    { FileMetadata.XsltKey, Path.Combine(Environment.CurrentDirectory, "Template.xslt") },
                    { Constants.PropertyName.Title, "Global Header" },
                };

                Should("generate html content with overridden title", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Title], Is.EqualTo("Global Header"));
                });
            });

            And("there is a yaml header specified", () =>
            {
                yamlHeader = new Dictionary<string, object>
                {
                    { Constants.PropertyName.Uid, "some-uid" },
                    { Constants.PropertyName.DocumentType, "Conceptual" },
                    { Constants.PropertyName.OutputFileName, "output-file-name.html" },
                    { Constants.PropertyName.Title, "Some Title" },
                    { "any_other_key", "any-other-value" },
                }.ToImmutableDictionary();

                Should("generate html content with properties specified in the yaml header", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Uid], Contains.Substring("some-uid"));
                    Assert.That(result.Uids[0].Name, Is.EqualTo("some-uid"));
                    Assert.That(result.Uids[0].File, Is.EqualTo(models[0].LocalPathFromRoot));

                    Assert.That(content[Constants.PropertyName.DocumentType], Is.EqualTo("Conceptual"));
                    Assert.That(result.DocumentType, Is.EqualTo("Conceptual"));

                    Assert.That(content[Constants.PropertyName.OutputFileName], Is.EqualTo("output-file-name.html"));
                    Assert.That(result.File, Is.EqualTo("output-file-name.html"));

                    Assert.That(content["any_other_key"], Is.EqualTo("any-other-value"));
                });

                And("the yaml header has incorrect output file name", () =>
                {
                    yamlHeader = new Dictionary<string, object>
                    {
                        { Constants.PropertyName.OutputFileName, "extra-path/output-file-name.html" },
                    }.ToImmutableDictionary();

                    Should("generate html content", () =>
                    {
                        Assert.That(result.File, Is.EqualTo(models[0].File));
                    });
                });
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocBuildStep.Build"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocBuildStep.Build))]
    public static void Build()
    {
        var xmlDocBuildStep = new XmlDocBuildStep();
        var hostMock = new Mock<IHostService>();

        When("the method is called", () =>
        {
            Act(() =>
            {
                xmlDocBuildStep.Build(null, hostMock.Object);
            });

            Should("do nothing", () =>
            {
                Assert.That(hostMock.Invocations, Has.Count.Zero);
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocBuildStep.Postbuild"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocBuildStep.Postbuild))]
    public static void Postbuild()
    {
        var xmlDocBuildStep = new XmlDocBuildStep();
        var hostMock = new Mock<IHostService>();

        When("the method is called", () =>
        {
            Act(() =>
            {
                xmlDocBuildStep.Postbuild(null, hostMock.Object);
            });

            Should("do nothing", () =>
            {
                Assert.That(hostMock.Invocations, Has.Count.Zero);
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocBuildStep.BuildOrder"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocBuildStep.BuildOrder))]
    public static void BuildOrder()
    {
        var xmlDocBuildStep = new XmlDocBuildStep();

        When("the property is called", () =>
        {
            Should("return zero", () =>
            {
                Assert.That(xmlDocBuildStep.BuildOrder, Is.Zero);
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocBuildStep.Name"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocBuildStep.Name))]
    public static void Name()
    {
        var xmlDocBuildStep = new XmlDocBuildStep();

        When("the property is called", () =>
        {
            Should("return the class name of the build step", () =>
            {
                Assert.That(xmlDocBuildStep.Name, Is.EqualTo(nameof(XmlDocBuildStep)));
            });
        });
    }
}
