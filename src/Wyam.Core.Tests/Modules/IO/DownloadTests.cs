﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using Wyam.Common.Documents;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Pipelines;
using Wyam.Core.Documents;
using Wyam.Core.Meta;
using Wyam.Core.Modules.IO;
using Wyam.Testing;

namespace Wyam.Core.Tests.Modules.IO
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class DownloadTests : BaseFixture
    {
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static byte[] ReadToByte(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public class ExecuteMethodTests : DownloadTests
        {
            [Test]
            public void SingleHtmlDownloadGetStream()
            {
                IDocument document = Substitute.For<IDocument>();
                Stream stream = null;
                IEnumerable<KeyValuePair<string, object>> metadata = null;
                string source = null;
                IModule download = new Download().WithUris("http://www.siwawi.com/");
                IExecutionContext context = Substitute.For<IExecutionContext>();
                context
                    .When(x => x.GetDocument(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<IEnumerable<KeyValuePair<string, object>>>(), Arg.Any<bool>()))
                    .Do(x =>
                    {
                        source = x.Arg<string>();
                        stream = x.Arg<Stream>();
                        metadata = x.Arg<IEnumerable<KeyValuePair<string, object>>>();
                    });

                // When
                download.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then

                Assert.IsNotEmpty(source, "Source cannot be empty");

                var headers = metadata.FirstOrDefault(x => x.Key == Keys.SourceHeaders).Value as Dictionary<string, string>;

                Assert.IsNotNull(headers, "Header cannot be null");
                Assert.IsTrue(headers.Count > 0, "Headers must contain contents");

                foreach (var h in headers)
                {
                    Assert.IsNotEmpty(h.Key, "Header key cannot be empty");
                    Assert.IsNotEmpty(h.Value, "Header value cannot be empty");
                }

                stream.Seek(0, SeekOrigin.Begin);
                var content = new StreamReader(stream).ReadToEnd();
                stream.Dispose();

                Assert.IsNotEmpty(content, "Download cannot be empty");
            }

            [Test]
            public void MultipleHtmlDownload()
            {
                IDocument document = Substitute.For<IDocument>();

                var output = new List<Tuple<Stream, IEnumerable<KeyValuePair<string, object>>>>();

                IExecutionContext context = Substitute.For<IExecutionContext>();
                context
                    .When(x => x.GetDocument(Arg.Any<Stream>(), Arg.Any<IEnumerable<KeyValuePair<string, object>>>(), Arg.Any<bool>()))
                    .Do(x =>
                    {
                        output.Add(Tuple.Create(x.Arg<Stream>(), x.Arg<IEnumerable<KeyValuePair<string, object>>>()));
                    });

                IModule download = new Download().WithUris("http://www.siwawi.com/", "http://stackoverflow.com/questions/221925/creating-a-byte-array-from-a-stream");

                // When
                download.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list
            
                // Then
                foreach(var o in output)
                {
                    var headers = o.Item2.FirstOrDefault(x => x.Key == Keys.SourceHeaders).Value as Dictionary<string, string>;

                    Assert.IsNotNull(headers, "Header cannot be null");
                    Assert.IsTrue(headers.Count > 0, "Headers must contain contents");

                    foreach (var h in headers)
                    {
                        Assert.IsNotEmpty(h.Key, "Header key cannot be empty");
                        Assert.IsNotEmpty(h.Value, "Header value cannot be empty");
                    }

                    o.Item1.Seek(0, SeekOrigin.Begin);
                    var content = new StreamReader(o.Item1).ReadToEnd();
                    o.Item1.Dispose();

                    Assert.IsNotEmpty(content, "Download cannot be empty");
                }
            }

            [Test]
            public void SingleImageDownload()
            {
                IDocument document = Substitute.For<IDocument>();
                Stream stream = null;
                IEnumerable<KeyValuePair<string, object>> metadata = null;

                IExecutionContext context = Substitute.For<IExecutionContext>();
                context
                    .When(x => x.GetDocument(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<IEnumerable<KeyValuePair<string, object>>>()))
                    .Do(x =>
                    {
                        stream = x.Arg<Stream>();
                        metadata = x.Arg<IEnumerable<KeyValuePair<string, object>>>();
                    });

                IModule download = new Download().WithUris("http://siwawi.com/images/cover/617215_113386155490459_1547184305_o-cover.jpg");
                context.OutputFolder.Returns(x => AssemblyDirectory);

                // When
                download.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                stream.Seek(0, SeekOrigin.Begin);

                var path = Path.Combine(context.OutputFolder, "test.jpg");
                File.WriteAllBytes(path, ReadToByte(stream));
                stream.Dispose();

                Assert.IsTrue(File.Exists(path), "Download cannot be empty");
            }

            [Test]
            public void SingleImageDownloadWithRequestHeader()
            {
                IDocument document = Substitute.For<IDocument>();
                Stream stream = null;
                IEnumerable<KeyValuePair<string, object>> metadata = null;

                IExecutionContext context = Substitute.For<IExecutionContext>();
                context
                    .When(x => x.GetDocument(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<IEnumerable<KeyValuePair<string, object>>>()))
                    .Do(x =>
                    {
                        stream = x.Arg<Stream>();
                        metadata = x.Arg<IEnumerable<KeyValuePair<string, object>>>();
                    });

                var header = new RequestHeader();
                header.Accept.Add("image/jpeg");

                IModule download = new Download().WithUri("http://siwawi.com/images/cover/617215_113386155490459_1547184305_o-cover.jpg", header);
                context.OutputFolder.Returns(x => AssemblyDirectory);

                // When
                download.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                stream.Seek(0, SeekOrigin.Begin);

                var path = Path.Combine(context.OutputFolder, "test-with-request-header.jpg");
                File.WriteAllBytes(path, ReadToByte(stream));
                stream.Dispose();

                Assert.IsTrue(File.Exists(path), "Download cannot be empty");
            }
        }
    }
}
