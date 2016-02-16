﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Wyam.Common.Configuration;
using Wyam.Common.Documents;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Pipelines;
using Wyam.Core.Documents;
using Wyam.Core.Meta;

namespace Wyam.Core.Modules.Metadata
{
    /// <summary>
    /// Optimizes a specified metadata key as a filename.
    /// </summary>
    /// <remarks>
    /// This module takes the value of the specified metadata key and optimizes it
    /// for use as a filename by removing reserved characters, white-listing characters,
    /// etc.
    /// </remarks>
    /// <metadata name="WriteFileName" type="string">Contains the optimized filename (unless
    /// an alternate metadata key was specified).</metadata>
    /// <metadata name="WritePath" type="string">Contains the full path with the optimized filename (unless
    /// an alternate metadata key was specified).</metadata>
    /// <category>Metadata</category>
    public class FileName : IModule
    {
	    private readonly List<string> _allowedCharacters = new List<string>();

	    internal static readonly string[] ReservedChars = new string[]
        {
            "-", "_", "~", ":", "/", "?", "#", "[", "]",
            "@", "!", "$", "&", "'", "(", ")", "*", "+", ",",
            ";", "=", "}", ";"
        };

        private static readonly Regex FileNameRegex = new Regex("^([a-zA-Z0-9])+$");

        private readonly DocumentConfig _fileName = (d, c) => d.String(Keys.SourceFileName);
        private readonly string _outputKey = Keys.WriteFileName;
        private string _pathOutputKey = Keys.WritePath;  // null for no output path
        
        /// <summary>
        /// Sets the metadata key <c>WriteFileName</c> to an optimized version of <c>SourceFileName</c>.
        /// Also sets the metadata key <c>WritePath</c> to <c>Path.Combine(RelativeFileDir, WriteFileName)</c>.
        /// </summary>
        public FileName()
        {
        }
        
        /// <summary>
        /// Sets the metadata key <c>WriteFileName</c> to an optimized version of the specified input metadata key.
        /// Also sets the metadata key <c>WritePath</c> to <c>Path.Combine(RelativeFileDir, WriteFileName)</c>.
        /// </summary>
        /// <param name="inputKey">The metadata key to use for the input filename.</param>
        public FileName(string inputKey)
        {
            if (inputKey == null)
            {
                throw new ArgumentNullException(nameof(inputKey));
            }
            if (string.IsNullOrWhiteSpace(inputKey))
            {
                throw new ArgumentException(nameof(inputKey));
            }
            _fileName = (d, c) => d.String(inputKey);
        }

        /// <summary>
        /// Sets the metadata key <c>WriteFileName</c> to an optimized version of the return value of the delegate.
        /// Also sets the metadata key <c>WritePath</c> to <c>Path.Combine(RelativeFileDir, WriteFileName)</c>.
        /// </summary>
        /// <param name="fileName">A delegate that should return a <c>string</c> with the filename to optimize.</param>
        public FileName(DocumentConfig fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            _fileName = fileName;
        }

        /// <summary>
        /// Sets the specified metadata key to an optimized version of the specified input metadata key.
        /// Does not automatically set the <c>WritePath</c> metadata key.
        /// </summary>
        /// <param name="inputKey">The metadata key to use for the input filename.</param>
        /// <param name="outputKey">The metadata key to use for the optimized filename.</param>
        public FileName(string inputKey, string outputKey)
        {
            if (inputKey == null)
            {
                throw new ArgumentNullException(nameof(inputKey));
            }
            if (string.IsNullOrWhiteSpace(inputKey))
            {
                throw new ArgumentException(nameof(inputKey));
            }
            if (outputKey == null)
            {
                throw new ArgumentNullException(nameof(outputKey));
            }
            if (string.IsNullOrWhiteSpace(outputKey))
            {
                throw new ArgumentException(nameof(outputKey));
            }
            _fileName = (d, c) => d.String(inputKey);
            _outputKey = outputKey;
        }

        /// <summary>
        /// Sets the specified metadata key to an optimized version of the return value of the delegate.
        /// Does not automatically set the <c>WritePath</c> metadata key.
        /// </summary>
        /// <param name="fileName">A delegate that should return a <c>string</c> with the filename to optimize.</param>
        /// <param name="outputKey">The metadata key to use for the optimized filename.</param>
        public FileName(DocumentConfig fileName, string outputKey)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (outputKey == null)
            {
                throw new ArgumentNullException(nameof(outputKey));
            }
            if (string.IsNullOrWhiteSpace(outputKey))
            {
                throw new ArgumentException(nameof(outputKey));
            }
            _fileName = fileName;
            _outputKey = outputKey;
        }

        /// <summary>
        /// Indicates whether to set the metadata key <c>WritePath</c> to <c>Path.Combine(RelativeFileDir, WriteFileName)</c>.
        /// </summary>
        /// <param name="preservePath">If set to <c>true</c>, the <c>WritePath</c> metadata key is set.</param>
        /// <returns></returns>
        public FileName PreservePath(bool preservePath)
        {
            if (!preservePath)
            {
                _pathOutputKey = null;
            }
            return this;
        }
        
        /// <summary>
        /// Indicates whether to set the specified metadata key to <c>Path.Combine(RelativeFileDir, WriteFileName)</c>.
        /// </summary>
        /// <param name="outputKey">The metadata key to set.</param>
        public FileName PreservePath(string outputKey)
        {
            if (outputKey == null)
            {
                throw new ArgumentNullException(nameof(outputKey));
            }
            if (string.IsNullOrWhiteSpace(outputKey))
            {
                throw new ArgumentException(nameof(outputKey));
            }
            _pathOutputKey = outputKey;
            return this;
        }

        /// <summary>
        /// Specifies the characters to allow in the filename.
        /// </summary>
        /// <param name="allowedCharacters">The allowed characters.</param>
        /// <returns></returns>
        public FileName WithAllowedCharacters(IEnumerable<string> allowedCharacters)
        {
            _allowedCharacters.AddRange(allowedCharacters);
            return this;
        }

        public IEnumerable<IDocument> Execute(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            return inputs.AsParallel().Select(input =>
            {
                string fileName = _fileName.Invoke<string>(input, context);

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = GetFileName(fileName);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        string relativeFileDir = input.String(Keys.RelativeFileDir);
                        if (!string.IsNullOrWhiteSpace(_pathOutputKey) && !string.IsNullOrWhiteSpace(relativeFileDir))
                        {
							return context.GetDocument(input, new MetadataItems
                            {
                                { _outputKey, fileName },
                                { _pathOutputKey, Path.Combine(relativeFileDir, fileName) }
                            });
                        }
                        return context.GetDocument(input, new MetadataItems
                        {
                            { _outputKey, fileName }
                        });
                    }
                }
                return input;
            });
        }

		private string GetFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            // Trim whitespace
		    fileName = fileName.Trim();

            // Remove multiple dashes
            fileName = Regex.Replace(fileName, @"\-{2,}", "");

            // Remove reserved chars - doing this as an array reads a lot better than a regex
            foreach (string token in ReservedChars.Except(_allowedCharacters))
            {
                fileName = fileName.Replace(token, "");
            }

            // Trim dot (special case, only reserved if at beginning or end)
		    if (!_allowedCharacters.Contains("."))
		    {
		        fileName = fileName.Trim('.');
		    }

            // Remove multiple spaces
            fileName = Regex.Replace(fileName, @"\s+", " ");

            // Turn spaces into dashes
            fileName = fileName.Replace(" ", "-");

            // Grab letters and numbers only, use a regex to be unicode-friendly
            if (FileNameRegex.IsMatch(fileName))
            {
                fileName = FileNameRegex.Matches(fileName)[0].Value;
            }

			// Urls should not be case-sensitive
			fileName = fileName.ToLowerInvariant();

            return fileName;
        }
    }
}
