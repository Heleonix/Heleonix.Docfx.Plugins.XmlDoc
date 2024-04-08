// <copyright file="Transformer.cs" company="Heleonix - Hennadii Lutsyshyn">
// Copyright (c) Heleonix - Hennadii Lutsyshyn. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the repository root for full license information.
// </copyright>

namespace Heleonix.Docfx.Plugins.XmlDoc;

using System.Collections.Concurrent;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;
using System.Xml.Xsl;
using global::Docfx.Plugins;
using RazorEngineCore;

/// <inheritdoc/>>
[Export(nameof(ITransformer), typeof(ITransformer))]
public class Transformer : ITransformer
{
    private static readonly RazorEngine Engine = new ();

    private readonly ConcurrentDictionary<string, XslCompiledTransform> xmlTemplates = new ();

    private readonly ConcurrentDictionary<string, IRazorEngineCompiledTemplate<RazorEngineTemplateBase<XDocument>>>
        razorTemplates = new ();

    static Transformer()
    {
        AssemblyLoadContext.Default.Resolving += Transformer.Default_Resolving;
    }

    /// <inheritdoc/>
    public string Transform(FileModel model, IHostService host)
    {
        try
        {
            var content = (IDictionary<string, object>)model.Content;

            var metadata = FileMetadata.From(content);

            if (".xslt".Equals(Path.GetExtension(metadata.Template), StringComparison.OrdinalIgnoreCase))
            {
                return this.TransformWithXslt(model, metadata);
            }
            else if (".cshtml".Equals(Path.GetExtension(metadata.Template), StringComparison.OrdinalIgnoreCase))
            {
                return this.TransformWithRazor(model, metadata);
            }

            host.LogError($"Unknown template file format: {metadata.Template}.", model.FileAndType.FullPath);

            return null;
        }
        catch (Exception ex)
        {
            host.LogError(ex.ToString(), model.FileAndType.FullPath);

            throw;
        }
    }

    /// <summary>
    /// Resolves dependencies manually, because MEF2 in Docfx does not handle plugin's dependencies.
    /// In unit tests dependencies are resolved automatically, so this method does not need to be covered by tests.
    /// </summary>
    /// <param name="context">The assembly load context. Not used.</param>
    /// <param name="assemblyName">The name of the dependency assembly to load manually.</param>
    /// <returns>Returns the resolved assembly, which is dependency for this plugin, otherwise null.</returns>
    [ExcludeFromCodeCoverage]
    private static Assembly Default_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
#pragma warning disable S3885 // "Assembly.Load" should be used
        if (assemblyName.Name.Equals("RazorEngineCore", StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "RazorEngineCore.dll");

            return Assembly.LoadFile(path);
        }

        if (assemblyName.Name.Equals("HtmlAgilityPack", StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "HtmlAgilityPack.dll");

            return Assembly.LoadFile(path);
#pragma warning restore S3885 // "Assembly.Load" should be used
        }

        return null;
    }

    private string TransformWithXslt(FileModel model, FileMetadata metadata)
    {
        var template = this.xmlTemplates.GetOrAdd(
            metadata.Template,
            (k, arg) =>
            {
                var t = new XslCompiledTransform();
                t.Load(arg);
                return t;
            }, metadata.Template);

        using var stringWriter = new StringWriter();

        var args = new XsltArgumentList();

        args.AddParam("filename", string.Empty, Path.GetFileNameWithoutExtension(model.File));

        template.Transform(model.FileAndType.FullPath, args, stringWriter);

        return stringWriter.ToString();
    }

    private string TransformWithRazor(FileModel model, FileMetadata metadata)
    {
        var template = this.razorTemplates.GetOrAdd(
            metadata.Template,
            (k, arg) => Engine.Compile<RazorEngineTemplateBase<XDocument>>(File.ReadAllText(arg)),
            metadata.Template);

        return template.Run(instance =>
            instance.Model = XDocument.Load(model.FileAndType.FullPath, LoadOptions.SetBaseUri));
    }
}
