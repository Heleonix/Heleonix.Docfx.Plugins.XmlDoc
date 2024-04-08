// <copyright file="XmlDocBuildStepTests.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc.Tests;

using global::Docfx.Common;
using global::Docfx.DataContracts.Common;
using global::Docfx.Plugins;
using Heleonix.Docfx.Plugins.XmlDoc;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework.Constraints;
using System.Collections.Immutable;
using System.Reflection;

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
        var hostMock = new Mock<IHostService>();
        var transformerMock = new Mock<ITransformer>();
        var headerHandlerMock = new Mock<IHeaderHandler>();
        var tocHandlerMock = new Mock<ITocHandler>();
        var xmlDocBuildStep = new XmlDocBuildStep
        {
            Transformer = transformerMock.Object,
            HeaderHandler = headerHandlerMock.Object,
            TocHandler = tocHandlerMock.Object,
        };

        var fileAndType = new FileAndType("X:/BaseDir", "file.xsd", DocumentType.Article);
        ImmutableList<FileModel> models = null;
        XmlDocProcessor processor = null;
        FileModel result = null;

        Arrange(() =>
        {
            processor = new XmlDocProcessor();

            processor.GetProcessingPriority(fileAndType);

            var metadata = new Dictionary<string, object>
            {
                { FileMetadata.TemplateKey, Path.Combine(Environment.CurrentDirectory, "Template.xslt") },
            };

            var model = processor.Load(fileAndType, metadata.ToImmutableDictionary());

            model.Uids = new[] { new UidDefinition("some.uid", model.LocalPathFromRoot) }.ToImmutableArray();

            transformerMock.Setup((ITransformer t) => t.Transform(model, hostMock.Object))
                .Returns("Transformed MD").Verifiable();

            var markupResult = new MarkupResult
            {
                Html = "Markup Html",
                YamlHeader = new Dictionary<string, object>().ToImmutableDictionary(),
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

            hostMock.SetupGet((IHostService hs) => hs.Processor)
                .Returns(processor).Verifiable();
            hostMock.Setup((IHostService hs) => hs.Markup("Transformed MD", fileAndType, false))
                .Returns(markupResult).Verifiable();

            headerHandlerMock.Setup((IHeaderHandler hh) => hh.ExtractH1("Markup Html"))
                .Returns(("h1", "h1 raw", "Conceptual Content")).Verifiable();
            headerHandlerMock.Setup((IHeaderHandler hh) => hh.HandleYamlHeader(markupResult.YamlHeader, model))
                .Verifiable();
            headerHandlerMock.Setup((IHeaderHandler hh) => hh.GetTitle(model, markupResult.YamlHeader, "h1"))
                .Returns("Some Title").Verifiable();

            tocHandlerMock.Setup((ITocHandler th) => th.HandleTocRestructions(model, It.IsAny<IList<TreeItemRestructure>>()))
                .Verifiable();

            models = new List<FileModel> { model }.ToImmutableList();
        });

        When("the method is called", () =>
        {
            Act(() =>
            {
                result = xmlDocBuildStep.Prebuild(models, hostMock.Object).Single();
            });

            And("the content file model should be pre-built", () =>
            {
                Should("prebuild the passed model", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content["rawTitle"], Is.EqualTo("h1 raw"));
                    Assert.That(result.ManifestProperties.rawTitle, Is.EqualTo("h1 raw"));
                    Assert.That(content[Constants.PropertyName.Conceptual], Is.EqualTo("Conceptual Content"));
                    Assert.That(content[Constants.PropertyName.Title], Is.EqualTo("Some Title"));

                    Assert.That(result.LinkToFiles, Contains.Item("link1"));
                    Assert.That(result.LinkToUids, Contains.Item("uid1"));
                    Assert.That(result.FileLinkSources["key1"][0].SourceFile, Is.EqualTo("src1"));
                    Assert.That(result.UidLinkSources["key2"][0].SourceFile, Is.EqualTo("src2"));
                    Assert.That(result.Properties.XrefSpec.Uid, Is.EqualTo(result.Uids[0].Name));
                    Assert.That(result.Properties.XrefSpec.Name, Is.EqualTo("Some Title"));
                    Assert.That(
                        result.Properties.XrefSpec.Href,
                        Is.EqualTo((string)((RelativePath)result.File).GetPathFromWorkingFolder()));

                    hostMock.Verify();
                    transformerMock.Verify();
                    headerHandlerMock.Verify();
                    tocHandlerMock.Verify();
                });
            });

            And("there is an unrecognized content file model", () =>
            {
                Arrange(() =>
                {
                    processor.ContentFiles.Remove(fileAndType.File, out _);
                });

                Should("skip the unrecognized file", () =>
                {
                    var content = result.Content as IDictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Conceptual], Is.Empty);
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
