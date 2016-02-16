﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Wyam.Common.Documents;
using Wyam.Common.Pipelines;
using Wyam.Core.Documents;
using Wyam.Core.Meta;
using Wyam.Testing;

namespace Wyam.Core.Tests.Documents
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class CustomDocumentFactoryTests : BaseFixture
    {
        public class GetDocumentMethodTests : CustomDocumentFactoryTests
        {
            [Test]
            public void GetsInitialDocumentWithInitialMetadata()
            {
                // Given
                InitialMetadata initialMetadata = new InitialMetadata();
                initialMetadata.Add("Foo", "Bar");
                DocumentFactory documentFactory = new DocumentFactory(initialMetadata);
                CustomDocumentFactory<TestDocument> customDocumentFactory = new CustomDocumentFactory<TestDocument>(documentFactory);
                IExecutionContext context = Substitute.For<IExecutionContext>();

                // When
                IDocument resultDocument = customDocumentFactory.GetDocument(context);

                // Then
                Assert.IsInstanceOf<TestDocument>(resultDocument);
                CollectionAssert.AreEqual(new Dictionary<string, object>
                {
                    { "Foo", "Bar" }
                }, resultDocument);
            }

            [Test]
            public void ThrowsWhenCloneReturnsNullDocument()
            {
                // Given
                InitialMetadata initialMetadata = new InitialMetadata();
                DocumentFactory documentFactory = new DocumentFactory(initialMetadata);
                CustomDocumentFactory<TestDocument> customDocumentFactory = new CustomDocumentFactory<TestDocument>(documentFactory);
                IExecutionContext context = Substitute.For<IExecutionContext>();
                CloneReturnsNullDocument document = new CloneReturnsNullDocument();

                // When, Then
                Assert.Throws<Exception>(() => customDocumentFactory.GetDocument(context, document, new Dictionary<string, object>()));
            }

            [Test]
            public void ThrowsWhenCloneReturnsSameDocument()
            {
                // Given
                InitialMetadata initialMetadata = new InitialMetadata();
                DocumentFactory documentFactory = new DocumentFactory(initialMetadata);
                CustomDocumentFactory<TestDocument> customDocumentFactory = new CustomDocumentFactory<TestDocument>(documentFactory);
                IExecutionContext context = Substitute.For<IExecutionContext>();
                CloneReturnsSameDocument document = new CloneReturnsSameDocument();

                // When, Then
                Assert.Throws<Exception>(() => customDocumentFactory.GetDocument(context, document, new Dictionary<string, object>()));
            }

            [Test]
            public void CloneResultsInClonedDocument()
            {
                // Given
                InitialMetadata initialMetadata = new InitialMetadata();
                initialMetadata.Add("Foo", "Bar");
                DocumentFactory documentFactory = new DocumentFactory(initialMetadata);
                CustomDocumentFactory<TestDocument> customDocumentFactory = new CustomDocumentFactory<TestDocument>(documentFactory);
                IExecutionContext context = Substitute.For<IExecutionContext>();
                CustomDocument sourceDocument = (CustomDocument)customDocumentFactory.GetDocument(context);

                // When
                IDocument resultDocument = customDocumentFactory.GetDocument(context, sourceDocument, new Dictionary<string, object>
                {
                    { "Baz", "Bat" }
                });

                // Then
                CollectionAssert.AreEquivalent(new Dictionary<string, object>
                {
                    { "Foo", "Bar" }
                }, sourceDocument);
                CollectionAssert.AreEquivalent(new Dictionary<string, object>
                {
                    { "Foo", "Bar" },
                    { "Baz", "Bat" }
                }, resultDocument);
            }
        }

        private class TestDocument : CustomDocument
        {
            public string Title { get; set; }

            protected internal override CustomDocument Clone()
            {
                return new TestDocument
                {
                    Title = Title
                };
            }
        }

        private class CloneReturnsNullDocument : CustomDocument
        {
            protected internal override CustomDocument Clone()
            {
                return null;
            }
        }

        private class CloneReturnsSameDocument : CustomDocument
        {
            protected internal override CustomDocument Clone()
            {
                return this;
            }
        }
    }
}
