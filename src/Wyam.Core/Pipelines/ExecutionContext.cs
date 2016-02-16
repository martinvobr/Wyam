﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Wyam.Common.Caching;
using Wyam.Common.Documents;
using Wyam.Common.IO;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Pipelines;

namespace Wyam.Core.Pipelines
{
    internal class ExecutionContext : IExecutionContext
    {
        private readonly Pipeline _pipeline;

        public Engine Engine { get; }

        public byte[] RawConfigAssembly => Engine.RawConfigAssembly;

        public IEnumerable<Assembly> Assemblies => Engine.Assemblies;

        public IEnumerable<string> Namespaces => Engine.Namespaces;

        public IReadOnlyPipeline Pipeline => new ReadOnlyPipeline(_pipeline);

        public IModule Module { get; }

        public IDocumentCollection Documents => Engine.Documents;

        [Obsolete("This will be replaced by new IO functionality in the next release")]
        public string RootFolder => Engine.RootFolder;

        [Obsolete("This will be replaced by new IO functionality in the next release")]
        public string InputFolder => Engine.InputFolder;

        [Obsolete("This will be replaced by new IO functionality in the next release")]
        public string OutputFolder => Engine.OutputFolder;

        public IFileSystem FileSystem => Engine.FileSystem;

        public IExecutionCache ExecutionCache => Engine.ExecutionCacheManager.Get(Module);

        public string ApplicationInput => Engine.ApplicationInput;

        public ExecutionContext(Engine engine, Pipeline pipeline)
        {
            Engine = engine;
            _pipeline = pipeline;
        }

        private ExecutionContext(ExecutionContext original, IModule module)
        {
            Engine = original.Engine;
            _pipeline = original._pipeline;
            Module = module;
        }

        internal ExecutionContext Clone(IModule module)
        {
            return new ExecutionContext(this, module);
        }

        public bool TryConvert<T>(object value, out T result)
        {
            return TypeHelper.TryConvert(value, out result);
        }

        public IDocument GetDocument(string source, string content, IEnumerable<KeyValuePair<string, object>> items = null)
        {
            return GetDocument((IDocument)null, source, content, items);
        }

        public IDocument GetDocument(string content, IEnumerable<KeyValuePair<string, object>> items = null)
        {
            return GetDocument((IDocument)null, content, items);
        }

        public IDocument GetDocument(string source, Stream stream, IEnumerable<KeyValuePair<string, object>> items = null, bool disposeStream = true)
        {
            return GetDocument((IDocument)null, source, stream, items, disposeStream);
        }

        public IDocument GetDocument(Stream stream, IEnumerable<KeyValuePair<string, object>> items = null, bool disposeStream = true)
        {
            return GetDocument((IDocument)null, stream, items, disposeStream);
        }

        public IDocument GetDocument(IEnumerable<KeyValuePair<string, object>> items)
        {
            return GetDocument((IDocument)null, items);
        }

        // IDocumentFactory

        public IDocument GetDocument()
        {
            IDocument document = Engine.DocumentFactory.GetDocument(this);
            _pipeline.AddClonedDocument(document);
            return document;
        }

        public IDocument GetDocument(IDocument sourceDocument, string source, string content, IEnumerable<KeyValuePair<string, object>> items = null)
        {
            IDocument document = Engine.DocumentFactory.GetDocument(this, sourceDocument, source, content, items);
            if (sourceDocument != null && sourceDocument.Source == string.Empty)
            {
                // Only add a new source if the source document didn't already contain one (otherwise the one it contains will be used)
                _pipeline.AddDocumentSource(source);
            }
            _pipeline.AddClonedDocument(document);
            return document;
        }

        public IDocument GetDocument(IDocument sourceDocument, string content, IEnumerable<KeyValuePair<string, object>> items = null)
        {
            IDocument document = Engine.DocumentFactory.GetDocument(this, sourceDocument, content, items);
            _pipeline.AddClonedDocument(document);
            return document;
        }

        public IDocument GetDocument(IDocument sourceDocument, string source, Stream stream, IEnumerable<KeyValuePair<string, object>> items = null, bool disposeStream = true)
        {
            IDocument document = Engine.DocumentFactory.GetDocument(this, sourceDocument, source, stream, items, disposeStream);
            if (sourceDocument != null && sourceDocument.Source == string.Empty)
            {
                // Only add a new source if the source document didn't already contain one (otherwise the one it contains will be used)
                _pipeline.AddDocumentSource(source);
            }
            _pipeline.AddClonedDocument(document);
            return document;
        }

        public IDocument GetDocument(IDocument sourceDocument, Stream stream, IEnumerable<KeyValuePair<string, object>> items = null, bool disposeStream = true)
        {
            IDocument document = Engine.DocumentFactory.GetDocument(this, sourceDocument, stream, items, disposeStream);
            _pipeline.AddClonedDocument(document);
            return document;
        }

        public IDocument GetDocument(IDocument sourceDocument, IEnumerable<KeyValuePair<string, object>> items)
        {
            IDocument document = Engine.DocumentFactory.GetDocument(this, sourceDocument, items);
            _pipeline.AddClonedDocument(document);
            return document;
        }

        public IReadOnlyList<IDocument> Execute(IEnumerable<IModule> modules, IEnumerable<IDocument> inputs)
        {
            return Execute(modules, inputs, null);
        }

        // Executes the module with an empty document containing the specified metadata items
        public IReadOnlyList<IDocument> Execute(IEnumerable<IModule> modules, IEnumerable<KeyValuePair<string, object>> items = null)
        {
            return Execute(modules, null, items);
        }

        public IReadOnlyList<IDocument> Execute(IEnumerable<IModule> modules, IEnumerable<MetadataItem> items)
        {
            return Execute(modules, items?.Select(x => x.Pair));
        }

        private IReadOnlyList<IDocument> Execute(IEnumerable<IModule> modules, IEnumerable<IDocument> inputs, IEnumerable<KeyValuePair<string, object>> items)
        {
            if (modules == null)
            {
                return ImmutableArray<IDocument>.Empty;
            }

            // Store the document list before executing the child modules and restore it afterwards
            IReadOnlyList<IDocument> originalDocuments = Engine.DocumentCollection.Get(_pipeline.Name);
            ImmutableArray<IDocument> documents = inputs?.ToImmutableArray()
                ?? new[] { GetDocument(items) }.ToImmutableArray();
            IReadOnlyList<IDocument> results = _pipeline.Execute(this, modules, documents);
            Engine.DocumentCollection.Set(_pipeline.Name, originalDocuments);
            return results;
        }
    }
}
