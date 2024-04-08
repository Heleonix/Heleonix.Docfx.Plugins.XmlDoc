// <copyright file="XmlDocProcessorTests.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc.Tests;

using global::Docfx.DataContracts.Common;
using global::Docfx.Plugins;
using Moq;
using System.Collections.Immutable;
using System.Reflection;

/// <summary>
/// Tests the <see cref="XmlDocProcessor"/>.
/// </summary>
[ComponentTest(Type = typeof(XmlDocProcessor))]
public static class XmlDocProcessorTests
{
    /// <summary>
    /// Tests the <see cref="XmlDocProcessor.GetProcessingPriority(FileAndType)"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocProcessor.GetProcessingPriority))]
    public static void GetProcessingPriority()
    {
        FileAndType fileAndType = null;
        var priority = ProcessingPriority.NotSupported;
        var processor = new XmlDocProcessor();

        When("the method is called", () =>
        {
            Act(() =>
            {
                priority = processor.GetProcessingPriority(fileAndType);
            });

            And("the document type is supported", () =>
            {
                fileAndType = new FileAndType("C:/fake-base-dir", "some-file.xsd", DocumentType.Article);

                Should("return the normal processing priority", () =>
                {
                    Assert.That(priority, Is.EqualTo(ProcessingPriority.Normal));
                });
            });

            And("the document type is not supported", () =>
            {
                fileAndType = new FileAndType("C:/fake-base-dir", "some-file.xsd", DocumentType.Metadata);

                Should("return the not supported processing priority", () =>
                {
                    Assert.That(priority, Is.EqualTo(ProcessingPriority.NotSupported));
                });
            });

            And("the file type is not supported", () =>
            {
                fileAndType = new FileAndType("C:/fake-base-dir", "some-file.TXT", DocumentType.Article);

                Should("return the not supported processing priority", () =>
                {
                    Assert.That(priority, Is.EqualTo(ProcessingPriority.NotSupported));
                });
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocProcessor.Save(FileModel)"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocProcessor.Save))]
    public static void Save()
    {
        FileModel fileModel = null;
        var processor = new XmlDocProcessor();
        SaveResult result = null;

        When("the method is called", () =>
        {
            Act(() =>
            {
                result = processor.Save(fileModel);
            });

            And("the passed file model is not in the list of detected content files", () =>
            {
                fileModel = new FileModel(new FileAndType("D:/", "file.xml", DocumentType.Article), new object());

                Should("return null", () =>
                {
                    Assert.That(result, Is.Null);
                });
            });

            And("the passed file model is in the list of detected content files", () =>
            {
                processor.GetProcessingPriority(new FileAndType("D:/", "file.xml", DocumentType.Article));

                fileModel = new FileModel(new FileAndType("D:/", "file.xml", DocumentType.Article), new object())
                {
                    LinkToFiles = new HashSet<string> { "1", "2" }.ToImmutableHashSet(),
                    LinkToUids = new HashSet<string> { "3", "4" }.ToImmutableHashSet(),
                    FileLinkSources = new Dictionary<string, ImmutableList<LinkSourceInfo>>().ToImmutableDictionary(),
                    UidLinkSources = new Dictionary<string, ImmutableList<LinkSourceInfo>>().ToImmutableDictionary(),
                };
                fileModel.Properties.XrefSpec = null;

                Should("return the result representing the model", () =>
                {
                    Assert.That(result.DocumentType, Is.EqualTo("Conceptual"));
                    Assert.That(result.FileWithoutExtension, Is.EqualTo("file"));
                    Assert.That(result.LinkToFiles, Is.EqualTo(fileModel.LinkToFiles));
                    Assert.That(result.LinkToUids, Is.EqualTo(fileModel.LinkToUids));
                    Assert.That(result.FileLinkSources, Is.EqualTo(fileModel.FileLinkSources));
                    Assert.That(result.UidLinkSources, Is.EqualTo(fileModel.UidLinkSources));
                    Assert.That(result.XRefSpecs[0], Is.EqualTo(fileModel.Properties.XrefSpec));
                });

                And("the passed file model has XrefSpec", () =>
                {
                    fileModel.Properties.XrefSpec = new XRefSpec();

                    Should("return the result representing the model with XrefSpec", () =>
                    {
                        Assert.That(result.DocumentType, Is.EqualTo("Conceptual"));
                        Assert.That(result.FileWithoutExtension, Is.EqualTo("file"));
                        Assert.That(result.LinkToFiles, Is.EqualTo(fileModel.LinkToFiles));
                        Assert.That(result.LinkToUids, Is.EqualTo(fileModel.LinkToUids));
                        Assert.That(result.FileLinkSources, Is.EqualTo(fileModel.FileLinkSources));
                        Assert.That(result.UidLinkSources, Is.EqualTo(fileModel.UidLinkSources));
                        Assert.That(result.XRefSpecs[0], Is.EqualTo(fileModel.Properties.XrefSpec));
                    });
                });
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocProcessor.UpdateHref(FileModel, IDocumentBuildContext)"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocProcessor.UpdateHref))]
    public static void UpdateHref()
    {
        FileModel fileModel = null;
        FileAndType ft = null;
        var processor = new XmlDocProcessor();
        Mock<IDocumentBuildContext> ctxMock = null;

        When("the method is called", () =>
        {
            Act(() =>
            {
                ctxMock = new Mock<IDocumentBuildContext>();
                ft = new FileAndType("D:/", "file.xml", DocumentType.Article);
                fileModel = new FileModel(ft, new object());
                processor.UpdateHref(fileModel, ctxMock.Object);
            });

            Should("do nothing with the input arguments", () =>
            {
                Assert.That(ctxMock.Invocations, Has.Count.Zero);
                Assert.That(fileModel.DocumentType, Is.Null);
                Assert.That(fileModel.LinkToFiles, Is.Empty);
                Assert.That(fileModel.LinkToUids, Is.Empty);
                Assert.That(fileModel.FileLinkSources, Is.Empty);
                Assert.That(fileModel.UidLinkSources, Is.Empty);
                Assert.That(fileModel.FileAndType, Is.EqualTo(ft));
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocProcessor.Load(FileAndType, ImmutableDictionary{string, object})"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocProcessor.Load))]
    public static void Load()
    {
        FileAndType ft = null;
        var processor = new XmlDocProcessor();
        ImmutableDictionary<string, object> metadata = null;
        FileModel fileModel = null;

        When("the method is called", () =>
        {
            ft = new FileAndType(
                Environment.CurrentDirectory,
                Path.ChangeExtension(Path.GetRandomFileName(), ".xml"),
                DocumentType.Article);

            Act(() =>
            {
                fileModel = processor.Load(ft, metadata);
            });

            And("store is not specified", () =>
            {
                Arrange(() =>
                {
                    metadata = new Dictionary<string, object>
                    {
                        { "key1", 111 },
                        { "key2", 222 },
                        { FileMetadata.TemplateKey, "./relative/to/docfx-json/transform.xslt" },
                    }.ToImmutableDictionary();
                });

                Should("generate a file model without store details", () =>
                {
                    Assert.That(fileModel.LocalPathFromRoot, Is.EqualTo(ft.File));

                    var content = fileModel.Content as Dictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Conceptual], Is.Empty);
                    Assert.That(content[Constants.PropertyName.Type], Is.EqualTo(Constants.DocumentType.Conceptual));
                    Assert.That(content[Constants.PropertyName.Source], Is.Empty);
                    Assert.That(content[Constants.PropertyName.Path], Is.EqualTo(ft.File));

                    Assert.That(content["key1"], Is.EqualTo(111));
                    Assert.That(content["key2"], Is.EqualTo(222));
                    Assert.That(
                        content[FileMetadata.TemplateKey],
                        Is.EqualTo(Path.Combine(EnvironmentContext.BaseDirectory, metadata[FileMetadata.TemplateKey] as string)));

                    Assert.That(content[Constants.PropertyName.SystemKeys], Has.Length.EqualTo(8));

                    Assert.That(content[Constants.PropertyName.SystemKeys], Contains.Item(Constants.PropertyName.Conceptual));
                    Assert.That(content[Constants.PropertyName.SystemKeys], Contains.Item(Constants.PropertyName.Type));
                    Assert.That(content[Constants.PropertyName.SystemKeys], Contains.Item(Constants.PropertyName.Source));
                    Assert.That(content[Constants.PropertyName.SystemKeys], Contains.Item(Constants.PropertyName.Path));
                    Assert.That(content[Constants.PropertyName.SystemKeys], Contains.Item(Constants.PropertyName.Documentation));
                    Assert.That(content[Constants.PropertyName.SystemKeys], Contains.Item(Constants.PropertyName.Title));
                    Assert.That(content[Constants.PropertyName.SystemKeys], Contains.Item("rawTitle"));
                    Assert.That(content[Constants.PropertyName.SystemKeys], Contains.Item("wordCount"));
                });
            });

            And("store is specified", () =>
            {
                var tempStoreDir = Path.GetRandomFileName();

                processor.GetProcessingPriority(ft);

                Arrange(() =>
                {
                    File.WriteAllText(ft.FullPath, string.Empty);

                    Directory.CreateDirectory(tempStoreDir);

                    metadata = new Dictionary<string, object>
                    {
                        { FileMetadata.StoreKey, tempStoreDir },
                    }.ToImmutableDictionary();
                });

                Teardown(() =>
                {
                    Directory.Delete(tempStoreDir, true);
                });

                Should("generate a file model with store details", () =>
                {
                    Assert.That(fileModel.LocalPathFromRoot, Is.EqualTo($"{tempStoreDir}/{ft.File}"));
                    Assert.That(File.Exists(Path.Combine(Environment.CurrentDirectory, tempStoreDir, ft.File)), Is.True);
                });

                And("both original and stored files are detected", () =>
                {
                    processor.GetProcessingPriority(
                        new FileAndType(Environment.CurrentDirectory, Path.Combine(tempStoreDir, ft.File), DocumentType.Article));

                    Should("generate a file model with store details", () =>
                    {
                        Assert.That(fileModel.LocalPathFromRoot, Is.EqualTo($"{tempStoreDir}/{ft.File}"));
                        Assert.That(File.Exists(Path.Combine(Environment.CurrentDirectory, tempStoreDir, ft.File)), Is.True);
                    });
                });
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocProcessor.Name"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocProcessor.Name))]
    public static void Name()
    {
        var processor = new XmlDocProcessor();
        string name = null;

        When("the property is get", () =>
        {
            Act(() =>
            {
                name = processor.Name;
            });

            Should("return the name of the processor", () =>
            {
                Assert.That(name, Is.EqualTo(nameof(XmlDocProcessor)));
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="XmlDocProcessor.BuildSteps"/>.
    /// </summary>
    [MemberTest(Name = nameof(XmlDocProcessor.BuildSteps))]
    public static void BuildSteps()
    {
        var processor = new XmlDocProcessor();
        IEnumerable<IDocumentBuildStep> buildSteps = null;

        When("the property is get", () =>
        {
            Act(() =>
            {
                buildSteps = processor.BuildSteps;
            });

            Should("return the build steps as null", () =>
            {
                Assert.That(buildSteps, Is.Null);
            });
        });
    }
}