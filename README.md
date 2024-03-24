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

## Contribution Guideline

1. [Create a fork](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/fork) from the main repository
2. Implement whatever is needed
3. [Create a Pull Request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request-from-a-fork).
   Make sure the assigned [Checks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks#checks) pass successfully.
   You can watch the progress in the [PR: .NET](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/actions/workflows/pr-net.yml) GitHub workflows
4. [Request review](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/requesting-a-pull-request-review) from the code owner
5. Once approved, merge your Pull Request via [Squash and merge](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/incorporating-changes-from-a-pull-request/about-pull-request-merges#squash-and-merge-your-commits)

   > [!IMPORTANT]  
   > While merging, enter a [Conventional Commits](https://www.conventionalcommits.org/) commit message.
   > This commit message will be used in automatically generated [Github Release Notes](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/releases)
   > and [NuGet Release Notes](https://www.nuget.org/packages/Heleonix.Docfx.Plugins.XmlDoc/#releasenotes-body-tab)

6. Monitor the [Release: .NET / NuGet](https://github.com/Heleonix/Heleonix.Docfx.Plugins.XmlDoc/actions/workflows/release-net-nuget.yml) GitHub workflow to make sure your changes are delivered successfully
7. In case of any issues, please contact [heleonix.sln@gmail.com](mailto:heleonix.sln@gmail.com)