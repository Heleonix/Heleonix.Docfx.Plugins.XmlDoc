// <copyright file="TransformerTests.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc.Tests;

using global::Docfx.Plugins;
using Moq;

/// <summary>
/// Tests the <see cref="Transformer"/>.
/// </summary>
[ComponentTest(Type = typeof(Transformer))]
public static class TransformerTests
{
    /// <summary>
    /// Tests the <see cref="Transformer.Transform"/>.
    /// </summary>
    [MemberTest(Name = nameof(Transformer.Transform))]
    public static void Transform()
    {
        var transformer = new Transformer();
        FileModel model = null;
        Mock<IHostService> hostMock = null;
        var result = default(string);
        Exception exception = null;

        When("the method is called", () =>
        {
            Act(() =>
            {
                try
                {
                    result = transformer.Transform(model, hostMock.Object);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            And("the template format is 'xslt'", () =>
            {
                Arrange(() =>
                {
                    hostMock = new Mock<IHostService>();

                    model = new FileModel(
                        new FileAndType(Environment.CurrentDirectory, "Input.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                            { FileMetadata.TemplateKey, Path.Combine(Environment.CurrentDirectory, "Template.xslt") },
                        });
                });

                Should("successfully transform the xml-base file into markdown", () =>
                {
                    Assert.That(result, Contains.Substring("Hx_NetBuild_ArtifactsDir"));
                    Assert.That(result, Contains.Substring("Hx_NetBuild_SlnFile"));
                    Assert.That(result, Contains.Substring("Hx_NetBuild_SnkFile"));
                });
            });

            And("the template format is 'cshtml'", () =>
            {
                Arrange(() =>
                {
                    model = new FileModel(
                        new FileAndType(Environment.CurrentDirectory, "Input.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                            { FileMetadata.TemplateKey, Path.Combine(Environment.CurrentDirectory, "Template.cshtml") },
                        });
                });

                Should("successfully transform the xml-base file into markdown", () =>
                {
                    Assert.That(result, Contains.Substring("Hx_NetBuild_ArtifactsDir"));
                    Assert.That(result, Contains.Substring("Hx_NetBuild_SlnFile"));
                    Assert.That(result, Contains.Substring("Hx_NetBuild_SnkFile"));
                });
            });

            And("the template format is not recognized", () =>
            {
                Arrange(() =>
                {
                    model = new FileModel(
                        new FileAndType(Environment.CurrentDirectory, "Input.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                            { FileMetadata.TemplateKey, Path.Combine(Environment.CurrentDirectory, "Template.unknown") },
                        });

                    hostMock.Setup((IHostService hostService) => hostService.LogError(
                        It.Is<string>(s => s == $"Unknown template file format: {Path.Combine(Environment.CurrentDirectory, "Template.unknown")}."),
                        It.Is<string>(s => s == model.FileAndType.FullPath),
                        It.IsAny<string>())).Verifiable();
                });

                Should("log an error and return null", () =>
                {
                    Assert.That(result, Is.Null);
                    hostMock.Verify();
                });
            });

            And("some exception is thrown during transformation", () =>
            {
                Arrange(() =>
                {
                    model = new FileModel(
                        new FileAndType(Environment.CurrentDirectory, "Input.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                            { FileMetadata.TemplateKey, Path.Combine(Environment.CurrentDirectory, "NO_FILE.cshtml") },
                        });

                    hostMock.Setup((IHostService hostService) => hostService.LogError(
                        It.Is<string>(s => s.Contains("FileNotFoundException")),
                        It.Is<string>(s => s == model.FileAndType.FullPath),
                        It.IsAny<string>())).Verifiable();
                });

                Should("log an error and throw exception", () =>
                {
                    Assert.That(result, Is.Null);
                    Assert.That(exception, Is.Not.Null);
                    hostMock.Verify();
                });
            });
        });
    }
}