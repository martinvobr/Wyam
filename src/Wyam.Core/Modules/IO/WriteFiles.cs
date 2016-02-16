﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wyam.Common.Configuration;
using Wyam.Common.Documents;
using Wyam.Common.IO;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Pipelines;
using Wyam.Common.Tracing;
using Wyam.Core.Documents;
using Wyam.Core.Meta;

namespace Wyam.Core.Modules.IO
{
    /// <summary>
    /// Writes the content of each input document to the file system.
    /// </summary>
    /// <remarks>
    /// If the metadata keys <c>WriteFileName</c> (which requires <c>RelativeFileDir</c> to be 
    /// set, usually by the ReadFiles module), <c>WriteExtension</c> (which 
    /// requires <c>RelativeFilePath</c> to be set, usually by the <see cref="ReadFiles"/> module) 
    /// or <c>WritePath</c> are set on an input document, that value will be used instead 
    /// of what's specified in the module. For example, if you have a bunch 
    /// of Razor .cshtml files that need to be rendered to .html files but one of them 
    /// should be output as a .xml file instead, define the <c>WriteExtension</c> metadata value 
    /// in the front matter of the page.
    /// </remarks>
    /// <metadata name="DestinationFilePath" type="string">The full absolute path (including file name) 
    /// of the destination file.</metadata>
    /// <metadata name="DestinationFilePathBase" type="string">The full absolute path (including file name) 
    /// of the destination file without the file extension.</metadata>
    /// <metadata name="DestinationFileBase" type="string">The file name without any extension. Equivalent 
    /// to <c>Path.GetFileNameWithoutExtension(DestinationFilePath)</c>.</metadata>
    /// <metadata name="DestinationFileExt" type="string">The extension of the file. Equivalent 
    /// to <c>Path.GetExtension(DestinationFilePath)</c>.</metadata>
    /// <metadata name="DestinationFileName" type="string">The full file name. Equivalent 
    /// to <c>Path.GetFileName(DestinationFilePath)</c>.</metadata>
    /// <metadata name="DestinationFileDir" type="string">The full absolute directory of the file. 
    /// Equivalent to <c>Path.GetDirectoryName(DestinationFilePath)</c>.</metadata>
    /// <metadata name="RelativeFilePath" type="string">The relative path to the file (including file name)
    /// from the Wyam output folder.</metadata>
    /// <metadata name="RelativeFilePathBase" type="string">The relative path to the file (including file name)
    /// from the Wyam output folder without the file extension.</metadata>
    /// <metadata name="RelativeFileDir" type="string">The relative directory of the file 
    /// from the Wyam output folder.</metadata>
    /// <category>Input/Output</category>
    public class WriteFiles : IModule
    {
        private readonly DocumentConfig _path;
        private bool _warnOnWriteMetadata;
        private bool _useWriteMetadata = true;
        private bool _ignoreEmptyContent = true;
        private Func<IDocument, IExecutionContext, bool> _predicate = null;

        /// <summary>
        /// Uses a delegate to describe where to write the content of each document. 
        /// The output of the function should be either a full path to the disk 
        /// location (including file name) or a path relative to the output folder.
        /// </summary>
        /// <param name="path">A delegate that returns a <c>string</c> with the desired path.</param>
        public WriteFiles(DocumentConfig path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _path = path;
            _warnOnWriteMetadata = true;
        }

        /// <summary>
        /// Writes the document content to disk with the specified extension with the same 
        /// base file name and relative path as the input file. This requires metadata 
        /// for <c>RelativeFilePath</c> to be set (which is done by default by the <see cref="ReadFiles"/> module).
        /// </summary>
        /// <param name="extension">The extension to use for writing the file.</param>
        public WriteFiles(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            _path = (x, y) =>
            {
                string fileRelative = x.String(Keys.RelativeFilePath);
                if (!string.IsNullOrWhiteSpace(fileRelative))
                {
                    return System.IO.Path.ChangeExtension(fileRelative, extension);
                }
                return null;
            };
            _warnOnWriteMetadata = true;
        }

        /// <summary>
        /// Writes the document content to disk with the same file name and relative path 
        /// as the input file. This requires metadata for <c>RelativeFilePath</c> to be set 
        /// (which is done by default by the <see cref="ReadFiles"/> module).
        /// </summary>
        public WriteFiles()
        {
            _path = (x, y) => x.String(Keys.RelativeFilePath);
        }
        
        /// <summary>
        /// By default the metadata values for <c>WritePath</c>, <c>WriteFileName</c>, and <c>WriteExtension</c> 
        /// are checked and used first. This can be used to turn off the default behavior and always
        /// rely on the delegate for obtaining the write location.
        /// </summary>
        /// <param name="useWriteMetadata">If set to <c>false</c>, metadata of the input document will not be used.</param>
        public WriteFiles UseWriteMetadata(bool useWriteMetadata = true)
        {
            _useWriteMetadata = useWriteMetadata;
            return this;
        }

        /// <summary>
        /// Ignores documents with empty content, which is the default behavior.
        /// </summary>
        /// <param name="ignoreEmptyContent">If set to <c>true</c>, documents with empty content will be ignored.</param>
        public WriteFiles IgnoreEmptyContent(bool ignoreEmptyContent = true)
        {
            _ignoreEmptyContent = ignoreEmptyContent;
            return this;
        }

        /// <summary>
        /// Specifies a predicate that must be satisfied for the file to be written.
        /// </summary>
        /// <param name="predicate">A predicate that returns <c>true</c> if the file should be written.</param>
        public WriteFiles Where(DocumentConfig predicate)
        {
            Func<IDocument, IExecutionContext, bool> currentPredicate = _predicate;
            _predicate = currentPredicate == null
                ? (Func<IDocument, IExecutionContext, bool>)(predicate.Invoke<bool>)
                : ((x, c) => currentPredicate(x, c) && predicate.Invoke<bool>(x, c));
            return this;
        }

        protected bool ShouldProcess(IDocument input, IExecutionContext context)
        {
            return _predicate == null || _predicate(input, context);
        }

        protected string GetPath(IDocument input, IExecutionContext context)
        {
            string path = null;

            if (_useWriteMetadata)
            {
                string metadataKey = null;

                // WritePath
                path = input.String(Keys.WritePath);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    metadataKey = Keys.WritePath;
                    path = PathHelper.NormalizePath(path);
                }

                // WriteFileName
                if (string.IsNullOrWhiteSpace(path) && input.ContainsKey(Keys.WriteFileName)
                    && input.ContainsKey(Keys.RelativeFileDir))
                {
                    metadataKey = Keys.WriteFileName;
                    path = System.IO.Path.Combine(input.String(Keys.RelativeFileDir),
                        PathHelper.NormalizePath(input.String(Keys.WriteFileName)));
                }

                // WriteExtension
                if (string.IsNullOrWhiteSpace(path) && input.ContainsKey(Keys.WriteExtension)
                    && input.ContainsKey(Keys.RelativeFilePath))
                {
                    metadataKey = Keys.WriteExtension;
                    path = System.IO.Path.ChangeExtension(input.String(Keys.RelativeFilePath),
                        input.String(Keys.WriteExtension));
                }

                // Warn if needed
                if (metadataKey != null && _warnOnWriteMetadata)
                {
                    Trace.Warning("An extension or delegate was specified for the WriteFiles module, but the metadata key {0} took precedence for the document with source {1}."
                        + " Call UseWriteMetadata(false) to prevent the special write metadata keys from overriding WriteFiles constructor values.",
                        metadataKey, input.Source);
                }
            }

            // Func
            if (string.IsNullOrWhiteSpace(path))
            {
                path = _path.Invoke<string>(input, context);
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                path = System.IO.Path.GetFullPath(System.IO.Path.Combine(context.OutputFolder, path));
                if (!string.IsNullOrWhiteSpace(path))
                {
                    return path;
                }
            }

            return null;
        }

        public virtual IEnumerable<IDocument> Execute(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            return inputs.AsParallel().Select(input =>
            {
                if (ShouldProcess(input, context))
                {
                    string path = GetPath(input, context);
                    if (path != null)
                    {
                        string pathDirectory = System.IO.Path.GetDirectoryName(path);
                        if (pathDirectory != null && !Directory.Exists(pathDirectory))
                        {
                            Directory.CreateDirectory(pathDirectory);
                        }
                        FileStream outputStream;
                        using (Stream inputStream = input.GetStream())
                        {
                            if (_ignoreEmptyContent && inputStream.Length == 0)
                            {
                                return input;
                            }
                            outputStream = File.Open(path, FileMode.Create);
                            inputStream.CopyTo(outputStream);
                        }
                        Trace.Verbose("Wrote file {0}", path);
                        return context.GetDocument(input, outputStream, new MetadataItems
                        {
                            { Keys.DestinationFileBase, System.IO.Path.GetFileNameWithoutExtension(path) },
                            { Keys.DestinationFileExt, System.IO.Path.GetExtension(path) },
                            { Keys.DestinationFileName, System.IO.Path.GetFileName(path) },
                            { Keys.DestinationFileDir, System.IO.Path.GetDirectoryName(path) },
                            { Keys.DestinationFilePath, path },
                            { Keys.DestinationFilePathBase, PathHelper.RemoveExtension(path) },
                            { Keys.RelativeFilePath, PathHelper.GetRelativePath(context.OutputFolder, path) },
                            { Keys.RelativeFilePathBase, PathHelper.RemoveExtension(PathHelper.GetRelativePath(context.OutputFolder, path)) },
                            { Keys.RelativeFileDir, System.IO.Path.GetDirectoryName(PathHelper.GetRelativePath(context.OutputFolder, path)) }
                        });
                    }
                }
                return input;
            })
            .Where(x => x != null);
        }
    }
}
