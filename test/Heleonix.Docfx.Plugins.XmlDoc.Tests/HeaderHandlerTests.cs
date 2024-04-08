// <copyright file="HeaderHandlerTests.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc.Tests;

using global::Docfx.DataContracts.Common;
using System.Collections.Immutable;

/// <summary>
/// Tests the <see cref="HeaderHandler"/>.
/// </summary>
[ComponentTest(Type = typeof(HeaderHandler))]
public class HeaderHandlerTests
{
    /// <summary>
    /// Tests the <see cref="HeaderHandler.ExtractH1"/>.
    /// </summary>
    [MemberTest(Name = nameof(HeaderHandler.ExtractH1))]
    public void ExtractH1()
    {
        var headerHandler = new HeaderHandler();
        string html = null;
        var result = default((string h1, string h1Raw, string body));

        When("the method is called", () =>
        {
            Act(() =>
            {
                result = headerHandler.ExtractH1(html);
            });

            And("there is a full html content", () =>
            {
                Arrange(() =>
                {
                    html = "<!DOCTYPE html><html><head></head><body><h1>Heading</h1><p>Content</p></body></html>";
                });

                Should("extract 'h1' and body with 'h1'", () =>
                {
                    Assert.That(result.h1, Is.EqualTo("Heading"));
                    Assert.That(result.h1Raw, Is.Empty);
                    Assert.That(result.body, Is.EqualTo(html));
                });
            });

            And("the loaded html content is a fragment of html document", () =>
            {
                Arrange(() =>
                {
                    html = "     <h1>Heading</h1>     <p>Text</p>";
                });

                Should("extract 'h1' and body without 'h1'", () =>
                {
                    Assert.That(result.h1, Is.EqualTo("Heading"));
                    Assert.That(result.h1Raw, Is.EqualTo("<h1>Heading</h1>"));
                    Assert.That(result.body, Is.EqualTo("          <p>Text</p>"));
                });
            });

            And("there is no 'h1' html tag", () =>
            {
                Arrange(() =>
                {
                    html = "<p>Text</p>";
                });

                Should("return body", () =>
                {
                    Assert.That(result.h1, Is.Null);
                    Assert.That(result.h1Raw, Is.Empty);
                    Assert.That(result.body, Is.EqualTo("<p>Text</p>"));
                });
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="HeaderHandler.HandleYamlHeader"/>.
    /// </summary>
    [MemberTest(Name = nameof(HeaderHandler.HandleYamlHeader))]
    public void HandleYamlHeader()
    {
        var headerHandler = new HeaderHandler();
        ImmutableDictionary<string, object> yamlHeader = null;
        FileModel model = null;

        When("the method is called", () =>
        {
            Arrange(() =>
            {
                model = new FileModel(
                    new FileAndType("X:/Base", "some\\file.xsd", DocumentType.Article),
                    new Dictionary<string, object>());
            });

            Act(() =>
            {
                headerHandler.HandleYamlHeader(yamlHeader, model);
            });

            And("the yaml header is not provided", () =>
            {
                Arrange(() =>
                {
                    yamlHeader = null;
                    model = null;
                });

                Should("do nothing", () =>
                {
                    Assert.That(model, Is.Null);
                });
            });

            And("the yaml header is provided", () =>
            {
                yamlHeader = new Dictionary<string, object>
                {
                    { Constants.PropertyName.Uid, "some-uid" },
                    { Constants.PropertyName.DocumentType, "Conceptual" },
                    { Constants.PropertyName.OutputFileName, "output-file-name.html" },
                    { Constants.PropertyName.Title, "Some Title" },
                    { "any_other_key", "any-other-value" },
                }.ToImmutableDictionary();

                Should("populate the model with properties specified in the yaml header", () =>
                {
                    var content = model.Content as Dictionary<string, object>;

                    Assert.That(content[Constants.PropertyName.Uid], Contains.Substring("some-uid"));
                    Assert.That(model.Uids[0].Name, Is.EqualTo("some-uid"));
                    Assert.That(model.Uids[0].File, Is.EqualTo(model.LocalPathFromRoot));

                    Assert.That(content[Constants.PropertyName.DocumentType], Is.EqualTo("Conceptual"));
                    Assert.That(model.DocumentType, Is.EqualTo("Conceptual"));

                    Assert.That(content[Constants.PropertyName.OutputFileName], Is.EqualTo("output-file-name.html"));
                    Assert.That(model.File, Is.EqualTo("some/output-file-name.html"));

                    Assert.That(content["any_other_key"], Is.EqualTo("any-other-value"));
                });

                And("the yaml header has an incorrect output file name", () =>
                {
                    yamlHeader = new Dictionary<string, object>
                    {
                        { Constants.PropertyName.OutputFileName, "extra-path/output-file-name.html" },
                    }.ToImmutableDictionary();

                    Should("keep the File in the model unchanged", () =>
                    {
                        Assert.That(model.File, Is.EqualTo("some/file.xsd"));
                    });
                });
            });
        });
    }

    /// <summary>
    /// Tests the <see cref="HeaderHandler.GetTitle"/>.
    /// </summary>
    [MemberTest(Name = nameof(HeaderHandler.GetTitle))]
    public void GetTitle()
    {
        var headerHandler = new HeaderHandler();
        FileModel model = null;
        ImmutableDictionary<string, object> yamlHeader = null;
        string h1 = null;
        var result = default(string);

        When("the method is called", () =>
        {
            Act(() =>
            {
                result = headerHandler.GetTitle(model, yamlHeader, h1);
            });

            And("there is a yaml header specified with a title", () =>
            {
                Arrange(() =>
                {
                    yamlHeader = new Dictionary<string, object>
                    {
                        { Constants.PropertyName.Title, "Title" },
                    }.ToImmutableDictionary();
                });

                Should("return a title from the yaml header", () =>
                {
                    Assert.That(result, Is.EqualTo("Title"));
                });
            });

            And("the title is specified in metadata/titleOverwriteH1", () =>
            {
                Arrange(() =>
                {
                    yamlHeader = null;

                    model = new FileModel(
                        new FileAndType("X:/Base", "file.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                            { Constants.PropertyName.TitleOverwriteH1, "Title 1" },
                        });
                });

                Should("return a title from the metadata/titleOverwriteH1", () =>
                {
                    Assert.That(result, Is.EqualTo("Title 1"));
                });
            });

            And("the title is specified in global metadata/title", () =>
            {
                Arrange(() =>
                {
                    yamlHeader = null;

                    model = new FileModel(
                        new FileAndType("X:/Base", "file.xsd", DocumentType.Article),
                        new Dictionary<string, object>
                        {
                            { Constants.PropertyName.Title, "Title 2" },
                        });
                });

                Should("return a title from the metadata/title", () =>
                {
                    Assert.That(result, Is.EqualTo("Title 2"));
                });
            });

            And("the title is specified in the 'h1' parameter", () =>
            {
                Arrange(() =>
                {
                    yamlHeader = null;
                    model = new FileModel(
                        new FileAndType("X:/Base", "file.xsd", DocumentType.Article),
                        new Dictionary<string, object>());
                    h1 = "Title 3";
                });

                Should("return a title from the metadata/title", () =>
                {
                    Assert.That(result, Is.EqualTo("Title 3"));
                });
            });

            And("no title is specified", () =>
            {
                Arrange(() =>
                {
                    yamlHeader = null;
                    model = new FileModel(
                        new FileAndType("X:/Base", "file.xsd", DocumentType.Article),
                        new Dictionary<string, object>());
                    h1 = null;
                });

                Should("return a title as the file name", () =>
                {
                    Assert.That(result, Is.EqualTo("file"));
                });
            });
        });
    }
}
