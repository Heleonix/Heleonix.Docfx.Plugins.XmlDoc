# Heleonix.Docfx.Plugins.XmlDoc

The Docfx plugin to generate documentation from xml-based files via intermediate XSLT transformation into Markdown.

## Install

https://www.nuget.org/packages/Heleonix.Docfx.Plugins.XmlDoc

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
3. Configure the `docfx.json` with the plugin features. See the [Example].

### Example

Example of a configuration in a simple `docfx.json` file:

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
      "hx.xmldoc.xslt": { "**.xsd": "./xml-to-md.xslt" },
      "hx.xmldoc.store": { "../../some-external-location/*.xsd": "internal-store-folder" }
      "hx.xmldoc.toc": {
        "**/*-some.xsd": { "action": "InsertAfter", "key": "~/articles/introduction.md" },
        "**/*-other.xsd": { "action": "AppendChild", "key": "Namespace.Class.whatever.uid" }
      }
    }
  }
}
```

### File Metadata

`hx.xmldoc.xslt` - path to XSLT file to convert xml-based file to Markdown, which is then converted into output HTML by Docfx.

`hx.xmldoc.store` - a folder inside your documentation proejct, where the corresponding xml-based files are copied to
and then used as source files to generate output HTML from.
This is useful, when original files are not always available.
It works like metadata files generated from .NET projects/dlls/xml documentation.
Hrefs to such files can be specified as `internal-store-folder/your-file.xsd`.

`hx.xmldoc.toc` - specifies where and how the your xml-based files should be added into Table Of Contents.

`hx.xmldoc.toc / action` - one of the [TreeItemActionType](https://github.com/dotnet/docfx/blob/main/src/Docfx.Plugins/TreeItemActionType.cs)

`hx.xmldoc.toc / key` - a TOC item key to apply the action. If the key starts with `~`, then it is used as Href, otherwise as Uid.