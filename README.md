# Heleonix.Docfx.Plugins.XmlDoc

[![Release: .NET / NuGet](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/actions/workflows/release-net-nuget.yml/badge.svg)](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/actions/workflows/release-net-nuget.yml)

The Docfx plugin to generate documentation from xml-based files via intermediate template transformations into Markdown.

## Install

https://www.nuget.org/packages/Heleonix.Docfx.Plugins.XmlDoc

## Documentation

See [Heleonix.Docfx.Plugins.XmlDoc](https://heleonix.github.io/docs/Plugins/Heleonix.Docfx.Plugins.XmlDoc.html)

## Usage

1. Install the plugin and make it accessible for `Docfx` as a custom template. See [How to enable plugins](https://dotnet.github.io/docfx/tutorial/howto_build_your_own_type_of_documentation_with_custom_plug-in.html#enable-plug-in)
2. If needed, configure the plugin in the `Heleonix.Docfx.Plugins.XmlDoc.settings.json` located in the same folder as the
`Heleonix.Docfx.Plugins.XmlDoc.dll`:
    ```json
    {
      "SupportedFormats": [ ".xml", ".xsd", ".yourformat" ]
    }
    ```
    By default, `.xml` and `.xsd` file formats are recognized.
3. Configure the `docfx.json` with the plugin features. See the [docfx.json](#docfx.json).

### Examples

#### docfx.json

```json
{
  "build": {
    "content": [
      {
        "files": [ "**/*.{md,yml}" ],
        "exclude": [ "_site/**" ]
      },
      {
        "files": [ "*.xsd" ],
        "src": "../../some-external-location"
      },
      {
        "files": [ "internal-store-folder/*.xsd" ]
      }
    ],
    "resource": [
      {
        "files": [ "images/**" ]
      }
    ],
    "output": "_site",
    "template": [
      "default",
      "templates/template-with-xmldoc-plugin"
    ],
    "fileMetadata": {
      "hx.xmldoc.template": { "**.xsd": "./xml-to-md.xslt" },
      "hx.xmldoc.store": { "../../some-external-location/*.xsd": "internal-store-folder" }
      "hx.xmldoc.toc": {
        "**/*-some.xsd": { "action": "InsertAfter", "key": "~/articles/introduction.md" },
        "**/*-other.xsd": { "action": "AppendChild", "key": "Namespace.Class.whatever.uid" }
      }
    }
  }
}
```

#### input xml-based file, i.e. Hx_NetBuild.xsd

```xml
<?xml version="1.0" encoding="utf-8"?>

<xs:schema xmlns:msb="http://schemas.microsoft.com/developer/msbuild/2003"
           elementFormDefault="qualified"
           targetNamespace="http://schemas.microsoft.com/developer/msbuild/2003"
           xmlns:xs="http://www.w3.org/2001/XMLSchema">
  <xs:element name="Hx_NetBuild_ArtifactsDir" type="msb:StringPropertyType" substitutionGroup="msb:Property">
    <xs:annotation>
      <xs:documentation>A path to the NetBuild artifacts directory.</xs:documentation>
    </xs:annotation>
  </xs:element>
  <xs:element name="Hx_NetBuild_SlnFile" type="msb:StringPropertyType" substitutionGroup="msb:Property">
    <xs:annotation>
      <xs:documentation>A path to the solution file to build. Default is a .sln file found in the $Hx_WS_Dir.</xs:documentation>
    </xs:annotation>
  </xs:element>
  <xs:element name="Hx_NetBuild_SnkFile" type="msb:StringPropertyType" substitutionGroup="msb:Property">
    <xs:annotation>
      <xs:documentation>The file with public/private keys pair to sign assemblies.</xs:documentation>
    </xs:annotation>
  </xs:element>
</xs:schema>

```
#### xslt template

```xml
<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:xs="http://www.w3.org/2001/XMLSchema"
             xmlns:msb="http://schemas.microsoft.com/developer/msbuild/2003">
  <xsl:output method="text" indent="no"/>
  <xsl:strip-space elements="*"/>
  <xsl:param name="filename"/>
  <xsl:template match="/">
# <xsl:value-of select="$filename"/>
### Properties
<xsl:for-each select="xs:schema/xs:element[@substitutionGroup='msb:Property']">
#### <xsl:value-of select="@name"/>
<xsl:text>&#x0A;&#x0A;</xsl:text>
  <xsl:value-of select="normalize-space(xs:annotation/xs:documentation)"/>
<xsl:text>&#x0A;</xsl:text>
</xsl:for-each>
  </xsl:template>
</xsl:stylesheet>

```

#### cshtml template

```html
@using System
@using System.IO
@using System.Xml.Linq
@inherits RazorEngineCore.RazorEngineTemplateBase<XDocument>
@{
    XDocument model = Model;
    XNamespace xs = "http://www.w3.org/2001/XMLSchema";

    var fileName = Path.GetFileNameWithoutExtension(model.Document.BaseUri);
    var elements = model.Document.Element(xs + "schema").Elements(xs + "element");
    var props = elements.Where(e => e.Attribute("substitutionGroup")?.Value == "msb:Property");
}
---
uid: @fileName
---

# @fileName

@if (props.Count() > 0)
{
    <text>## Properties</text>
    @:
    foreach (var prop in props)
    {
        <text>#### @prop.Attribute("name").Value</text>
        @:
        <text>@prop.Element(xs + "annotation").Element(xs + "documentation").Value.Trim()</text>
        @:
    }
}
```

#### markdown output

```markdown
# Hx_NetBuild
### Properties

#### Hx_NetBuild_ArtifactsDir

A path to the NetBuild artifacts directory.

#### Hx_NetBuild_SlnFile

A path to the solution file to build. Default is a .sln file found in the $Hx_WS_Dir.

#### Hx_NetBuild_SnkFile

The file with public/private keys pair to sign assemblies.
```

### File Metadata

`hx.xmldoc.template` - path to a template `.cshtml` or `.xslt` file to transform xml-based file to Markdown, which is then converted into output HTML by Docfx.

`hx.xmldoc.store` - a folder inside your documentation proejct, where the corresponding xml-based files are copied to
and then used as source files to transform to intermediate markdown and generate output HTML from.
This is useful, when original files are not always available, i.e. when your single documentation project is applied
to different dotnet projects simultaneously to support multi-project documentation.
It works like metadata files generated from .NET projects/dlls/xml documentation, where generated `yaml` metadata could
be commited as part of your documentation project for future re-builds, when the original .NET project is not available.
Hrefs to such files can be specified as `internal-store-folder/your-file.xsd`.

`hx.xmldoc.toc` - specifies where and how the your xml-based files should be added into Table Of Contents.

`hx.xmldoc.toc / action` - one of the [TreeItemActionType](https://github.com/dotnet/docfx/blob/main/src/Docfx.Plugins/TreeItemActionType.cs)

`hx.xmldoc.toc / key` - a TOC item key to apply the action. If the key starts with `~`, then it is used as Href, otherwise as Uid.

## Contribution Guideline

1. [Create a fork](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/fork) from the main repository
2. Implement whatever is needed
3. [Create a Pull Request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request-from-a-fork).
   Make sure the assigned [Checks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks#checks) pass successfully.
   You can watch the progress in the [PR: .NET](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/actions/workflows/pr-net.yml) GitHub workflows
4. [Request review](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/requesting-a-pull-request-review) from the code owner
5. Once approved, merge your Pull Request via [Squash and merge](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/incorporating-changes-from-a-pull-request/about-pull-request-merges#squash-and-merge-your-commits)
   > **IMPORTANT**  
   > While merging, enter a [Conventional Commits](https://www.conventionalcommits.org/) commit message.
   > This commit message will be used in automatically generated [Github Release Notes](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/releases)
   > and [NuGet Release Notes](https://www.nuget.org/packages/Heleonix.Docfx.Plugins.XmlDoc/#releasenotes-body-tab)
6. Monitor the [Release: .NET / NuGet](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/actions/workflows/release-net-nuget.yml) GitHub workflow to make sure your changes are delivered successfully
7. In case of any issues, please contact [heleonix.sln@gmail.com](mailto:heleonix.sln@gmail.com)