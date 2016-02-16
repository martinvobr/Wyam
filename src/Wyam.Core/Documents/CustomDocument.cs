﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wyam.Common.Documents;
using Wyam.Common.Meta;

namespace Wyam.Core.Documents
{
    /// <summary>
    /// Derive custom document types from this class to get built-in support.
    /// </summary>
    public abstract class CustomDocument : IDocument
    {
        internal IDocument Document { get; set; }

        /// <summary>
        /// Clones this instance of the document. You must return a new instance of your 
        /// custom document type, even if nothing will change, otherwise the document factory 
        /// will throw an exception. The default implementation of this method performs a
        /// <code>object.MemberwiseClone()</code>.
        /// </summary>
        /// <returns>A new custom document instance with the same values as the current instance.</returns>
        protected internal virtual CustomDocument Clone() => (CustomDocument)MemberwiseClone();

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            Document.GetEnumerator();

        public int Count => Document.Count;

        public bool ContainsKey(string key) => Document.ContainsKey(key);

        public bool TryGetValue(string key, out object value) =>
            Document.TryGetValue(key, out value);

        public object this[string key] => Document[key];

        public IEnumerable<string> Keys => Document.Keys;

        public IEnumerable<object> Values => Document.Values;

        public IMetadata<T> MetadataAs<T>() => Document.MetadataAs<T>();

        public object Get(string key, object defaultValue = null) =>
            Document.Get(key, defaultValue);

        public T Get<T>(string key) => Document.Get<T>(key);

        public T Get<T>(string key, T defaultValue) =>
            Document.Get(key, defaultValue);

        public string String(string key, string defaultValue = null) =>
            Document.String(key, defaultValue);

        public IReadOnlyList<T> List<T>(string key, IReadOnlyList<T> defaultValue = null) =>
            Document.List(key, defaultValue);

        IDocument IMetadata.Document(string key) =>
            Document.Document(key);

        public IReadOnlyList<IDocument> Documents(string key) => Document.Documents(key);

        public string Link(string key, string defaultValue = null, bool pretty = true) =>
            Document.Link(key, defaultValue, pretty);

        public dynamic Dynamic(string key, object defaultValue = null) =>
            Document.Dynamic(key, defaultValue);

        public void Dispose() => Document.Dispose();

        public string Source => Document.Source;

        public string Id => Document.Id;

        public IMetadata Metadata => Document.Metadata;

        public string Content => Document.Content;

        public Stream GetStream() => Document.GetStream();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
